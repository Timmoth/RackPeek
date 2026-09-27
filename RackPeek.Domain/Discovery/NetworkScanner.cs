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

        var facts = new List<NetworkHostFact>(alive.Count);

        for (var i = 0; i < alive.Count; i++) {
            (var ip, var ping, List<int> open) = alive[i];
            facts.Add(new NetworkHostFact(ip, macByIp.GetValueOrDefault(ip), names[i], ping, open));
        }

        return facts
            .OrderBy(f => IpHelper.ToUInt32(f.Ip))
            .ToList();
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
