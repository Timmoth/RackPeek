using RackPeek.Domain.Resources.Services.Networking;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Sweeps a subnet and reports the hosts that answered. A host counts as alive when
///     it answers ping OR accepts a TCP connect on any probed port — plenty of gear
///     drops ICMP, and plenty of gear with ICMP open runs no interesting service, so
///     neither signal alone is enough. All IO goes through <see cref="INetworkProbe" />.
/// </summary>
public static class NetworkScanner {
    /// <summary>
    ///     The widest block a sweep accepts, wherever the block came from — typed by the
    ///     user or auto-detected off a NIC. Wider than this is 65k+ hosts: a typo or a
    ///     CGNAT/VPN prefix, not a homelab.
    /// </summary>
    public const int MinPrefix = 16;

    /// <summary>
    ///     Every address worth probing in the block: hosts only, so the network and
    ///     broadcast addresses are skipped — except in /31 (RFC 3021 point-to-point)
    ///     and /32, where every address is a host.
    /// </summary>
    public static IEnumerable<string> EnumerateTargets(Cidr cidr) {
        // 64-bit throughout: 1u << 32 wraps under C#'s masked shift, and a block that
        // touches 255.255.255.255 would overflow the loop bound in 32 bits.
        var size = 1UL << (32 - cidr.Prefix);

        var first = cidr.Prefix >= 31 ? cidr.Network : (ulong)cidr.Network + 1;
        var last = cidr.Prefix >= 31
            ? cidr.Network + size - 1
            : cidr.Network + size - 2;

        for (var ip = first; ip <= last; ip++)
            yield return IpHelper.ToIp((uint)ip);
    }

    public static async Task<IReadOnlyList<NetworkHostFact>> ScanAsync(
        INetworkProbe probe,
        NetworkScanOptions options,
        CancellationToken cancellationToken = default) {
        // Enforced here rather than only at a front end, so every caller — CLI flag,
        // auto-detected subnet, future MCP tool — hits the same wall.
        if (options.Cidr.Prefix < MinPrefix)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"/{options.Cidr.Prefix} is more than 65,534 hosts. Narrow the sweep to /{MinPrefix} or smaller.");

        var targets = EnumerateTargets(options.Cidr).ToList();

        using var gate = new SemaphoreSlim(options.Concurrency);

        (string Ip, bool Ping, List<int> Open)?[] swept = await Task.WhenAll(
            targets.Select(ip => SweepHostAsync(probe, options, ip, gate, cancellationToken)));

        var alive = swept.Where(h => h != null).Select(h => h!.Value).ToList();

        // Read the ARP table only after the sweep: it is the sweep's own pings and
        // connects that put the neighbours into it.
        IReadOnlyDictionary<string, string> macByIp =
            ArpTableParser.Parse(await probe.ReadArpAsync(cancellationToken));

        // Names resolve in parallel too — a resolver that drops PTR queries burns the
        // full timeout per lookup, and paying that once beats paying it per host.
        var names = await Task.WhenAll(alive.Select(async h => {
            await gate.WaitAsync(cancellationToken);

            try {
                return await probe.ReverseDnsAsync(h.Ip, options.DnsTimeout, cancellationToken);
            }
            finally {
                gate.Release();
            }
        }));

        // Interrogate the living: finish the port sweep the liveness check cut short,
        // then ask what they are. Only living hosts are asked, so the cost is a few
        // short connections per host rather than per address.
        (IReadOnlyList<int> Open, ServiceIdentity? Identity, IReadOnlyList<ServiceIdentity> Services)[] interrogated =
            options.IdentifyServices
                ? await Task.WhenAll(alive.Select(h =>
                    InterrogateAsync(probe, options, h.Ip, h.Open, gate, cancellationToken)))
                : [
                    .. alive.Select(h =>
                        ((IReadOnlyList<int>)h.Open, (ServiceIdentity?)null, (IReadOnlyList<ServiceIdentity>)[]))
                ];

        var facts = new List<NetworkHostFact>(alive.Count);

        for (var i = 0; i < alive.Count; i++) {
            (var ip, var ping, _) = alive[i];
            var mac = macByIp.GetValueOrDefault(ip);

            facts.Add(new NetworkHostFact(ip, mac, names[i], ping, interrogated[i].Open) {
                Identity = interrogated[i].Identity,
                Services = interrogated[i].Services,
                Vendor = MacVendorLookup.Lookup(mac)
            });
        }

        return facts
            .OrderBy(f => IpHelper.ToUInt32(f.Ip))
            .ToList();
    }

    /// <summary>Ports that speak TLS, where a certificate may name the machine.</summary>
    private static readonly HashSet<int> _tlsPorts = [443, 8443, 8006, 9443];

    /// <summary>Ports that speak plain HTTP, where a page title may name an application.</summary>
    private static readonly HashSet<int> _httpPorts = [80, 8080, 8000, 3000, 8123, 9000, 9090, 8096, 7860, 11434, 32400];

    /// <summary>
    ///     What to ask each open port, best evidence first. Only open ports are asked —
    ///     the sweep has just established which those are, and a handshake with a closed
    ///     port buys nothing but a timeout.
    ///     <para>
    ///         A certificate goes first because an X.509 common name is a host name by
    ///         construction. A page title is an application's name, which is a different
    ///         claim. An SSH greeting is last and names only the daemon.
    ///     </para>
    /// </summary>
    private static IEnumerable<(int Port, IdentitySource Source, bool Tls)> Candidates(
        IReadOnlyList<int> openPorts) {
        foreach (var port in openPorts.Where(_tlsPorts.Contains))
            yield return (port, IdentitySource.TlsCertificate, true);

        foreach (var port in openPorts.Where(_httpPorts.Contains))
            yield return (port, IdentitySource.Http, false);

        // Something is listening but nobody curated the port: it could be either, so
        // try both rather than assume.
        foreach (var port in openPorts.Where(p =>
                     p != 22 && !_tlsPorts.Contains(p) && !_httpPorts.Contains(p))) {
            yield return (port, IdentitySource.TlsCertificate, true);
            yield return (port, IdentitySource.Http, false);
        }

        if (openPorts.Contains(22))
            yield return (22, IdentitySource.SshBanner, false);
    }

    /// <summary>
    ///     Everything worth asking a host that has already proven it is alive: which of
    ///     the probed ports are open, and what its services say they are.
    ///     <para>
    ///         The liveness sweep stops at the first answer on purpose — it only needs to
    ///         know the host exists. That leaves the port list incomplete for a host that
    ///         answered, and empty for one that answered ping, so it is finished here. The
    ///         ports are worth having for their own sake (554 says camera, 445 says file
    ///         server) and they are the best possible targets for the banner probes below:
    ///         a service on a port nobody curated is exactly the one worth asking.
    ///     </para>
    /// </summary>
    private static async Task<(IReadOnlyList<int> Open, ServiceIdentity? Identity, IReadOnlyList<ServiceIdentity> Services)> InterrogateAsync(
        INetworkProbe probe,
        NetworkScanOptions options,
        string ip,
        IReadOnlyList<int> knownOpen,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        await gate.WaitAsync(cancellationToken);

        try {
            var open = new List<int>(knownOpen);

            // The liveness list plus the wider identity list: the host is alive, so the
            // extra connections are paid once for it rather than once per address.
            foreach (var port in options.Ports.Concat(options.IdentityPorts).Distinct()) {
                if (open.Contains(port))
                    continue;

                if (await probe.TryConnectAsync(ip, port, options.PortTimeout, cancellationToken))
                    open.Add(port);
            }

            open.Sort();

            (ServiceIdentity? identity, IReadOnlyList<ServiceIdentity> services) =
                await IdentifyAsync(probe, options, ip, open, cancellationToken);

            return (open, identity, services);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return (knownOpen, null, []);
        }
        finally {
            gate.Release();
        }
    }

    /// <summary>
    ///     Asks every open port what it is and sorts the answers into the two different
    ///     claims they make.
    ///     <para>
    ///         A certificate common name names the machine — that is what an X.509 CN is
    ///         for — so the first one found becomes the host's identity. A page title
    ///         names an application, which is a fact about what the host runs rather than
    ///         about the host, so each one becomes a Service instead. An SSH greeting
    ///         names only the daemon and is kept as a last-resort annotation.
    ///     </para>
    ///     Never throws: an unidentified host is still a found host.
    /// </summary>
    private static async Task<(ServiceIdentity? Identity, IReadOnlyList<ServiceIdentity> Services)> IdentifyAsync(
        INetworkProbe probe,
        NetworkScanOptions options,
        string ip,
        IReadOnlyList<int> openPorts,
        CancellationToken cancellationToken) {
        ServiceIdentity? host = null;
        ServiceIdentity? fallback = null;
        var services = new List<ServiceIdentity>();

        try {
            foreach ((var port, IdentitySource source, var tls) in Candidates(openPorts)) {
                // One application per port: a port that already answered has nothing
                // further to say, and asking again only costs time.
                if (services.Exists(s => s.Port == port))
                    continue;

                var name = source switch {
                    IdentitySource.TlsCertificate => ServiceIdentityParser.ParseTlsSubject(
                        await probe.ReadTlsSubjectAsync(ip, port, options.IdentifyTimeout, cancellationToken)),
                    IdentitySource.SshBanner => ServiceIdentityParser.ParseSshBanner(
                        await probe.ReadTcpBannerAsync(ip, port, options.IdentifyTimeout, cancellationToken)),
                    _ => ServiceIdentityParser.ParseHttpIdentity(
                        await probe.ReadHttpHeadAsync(ip, port, tls, options.IdentifyTimeout, cancellationToken))
                };

                if (name == null)
                    continue;

                var identity = new ServiceIdentity(name, source, port);

                switch (source) {
                    case IdentitySource.TlsCertificate:
                        host ??= identity;
                        break;

                    case IdentitySource.Http:
                        services.Add(identity);
                        break;

                    default:
                        fallback ??= identity;
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            // One host's probe timing out must not abandon the sweep, nor lose whatever
            // the earlier ports already said.
        }

        return (host ?? services.FirstOrDefault() ?? fallback, services);
    }

    /// <summary>One host's liveness check; null when nothing answered.</summary>
    private static async Task<(string Ip, bool Ping, List<int> Open)?> SweepHostAsync(
        INetworkProbe probe,
        NetworkScanOptions options,
        string ip,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        await gate.WaitAsync(cancellationToken);

        try {
            var ping = await probe.PingAsync(ip, options.PingTimeout, cancellationToken);
            var open = new List<int>();

            // Liveness needs one answer, not a port inventory: a ping reply skips the
            // port probes entirely, and probing stops at the first open port.
            if (!ping)
                foreach (var port in options.Ports) {
                    if (!await probe.TryConnectAsync(ip, port, options.PortTimeout, cancellationToken))
                        continue;

                    open.Add(port);
                    break;
                }

            return ping || open.Count > 0 ? (ip, ping, open) : null;
        }
        finally {
            gate.Release();
        }
    }
}
