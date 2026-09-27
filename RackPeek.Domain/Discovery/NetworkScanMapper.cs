using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Services.Networking;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Discovery;

/// <summary>Maps swept hosts onto the System resources RackPeek stores. Pure.</summary>
public static class NetworkScanMapper {
    public static List<Resource> ToResources(IReadOnlyList<NetworkHostFact> hosts) {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resources = new List<Resource>();

        foreach ((NetworkHostFact host, IReadOnlyList<string> allIps) in Collapse(hosts)) {
            // The MAC is the only identity a scan can see that survives a DHCP re-lease;
            // when ARP could not provide one (a routed subnet, say) the IP has to do,
            // and the id changes if the address does — documented in the guide.
            var discoveryId = DiscoveryId.Create(
                DiscoveryId.NetworkScheme,
                host.Mac ?? $"ip:{host.Ip}");

            // A PTR record is the network's own answer and wins. Failing that, whatever a
            // service volunteered beats a hash of the MAC — "pve-node-01" over "host-1a2b3c4d".
            var label = DiscoveryNaming.HostLabel(host.Hostname);

            if (string.IsNullOrEmpty(label) && NamesAHost(host.Identity))
                label = DiscoveryNaming.HostLabel(host.Identity!.Name);

            var system = new SystemResource {
                Kind = SystemResource.KindLabel,
                Name = DiscoveryNaming.Unique(
                    DiscoveryNaming.Suggest(
                        label,
                        "host",
                        discoveryId),
                    discoveryId,
                    taken),
                DiscoveryId = discoveryId,
                // Deliberately sparse: a scan sees an address, not an OS or a type, and
                // whatever it wrote here would overwrite the real values on every rescan
                // of a card the user (or an agent collector) has since filled in.
                Ip = host.Ip
            };

            if (host.Mac != null)
                system.Labels["mac"] = host.Mac;

            if (host.Vendor != null)
                system.Labels["vendor"] = host.Vendor;

            // The ports are observations, not conclusions: "554 is open" is a fact, while
            // "this is a camera" is an inference the reader is far better placed to make
            // than the scanner. Recording them keeps the evidence without inventing a
            // service that may not be what the port number conventionally implies.
            if (host.OpenPorts.Count > 0)
                system.Labels["open-ports"] = string.Join(",", host.OpenPorts);

            // Kept even when the name came from somewhere else: it records what the host
            // actually said, which is how someone judges whether the name is trustworthy.
            if (host.Identity != null)
                system.Labels["identified-by"] =
                    $"{Describe(host.Identity.Source)}:{host.Identity.Port} {host.Identity.Name}";

            if (allIps.Count > 1)
                system.Labels["ips"] = string.Join(",", allIps);

            resources.Add(system);

            // An application that named itself over HTTP is a fact about what the host
            // runs, not about what the host is, so it becomes a Service hanging off the
            // card rather than renaming it.
            foreach (ServiceIdentity found in host.Services) {
                // An appliance's own management UI is not a service running on it: a
                // firewall whose page says "OPNsense" on a card already called opnsense
                // would otherwise get a second card named opnsense-<hash>, which says
                // nothing the first one did not.
                if (DiscoveryNaming.Slug(DiscoveryNaming.HostLabel(found.Name))
                    .Equals(system.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var serviceId = DiscoveryId.Create(
                    DiscoveryId.NetworkScheme,
                    $"{host.Mac ?? $"ip:{host.Ip}"}:{found.Port}");

                resources.Add(new Service {
                    Kind = Service.KindLabel,
                    Name = DiscoveryNaming.Unique(
                        DiscoveryNaming.Suggest(
                            DiscoveryNaming.HostLabel(found.Name),
                            "service",
                            serviceId),
                        serviceId,
                        taken),
                    DiscoveryId = serviceId,
                    Network = new Network {
                        Ip = host.Ip,
                        Port = found.Port,
                        Protocol = "TCP"
                    },
                    RunsOn = [system.Name]
                });
            }
        }

        return resources;
    }

    /// <summary>
    ///     Whether an identity is a claim about the machine rather than about something
    ///     running on it. Only a certificate is: an X.509 common name is a host name by
    ///     construction, which is why a Proxmox node's certificate says "pve-node-01".
    ///     <para>
    ///         A page title names an application — and a host may run several, so naming
    ///         the machine after whichever answered first is arbitrary. Those become
    ///         Services instead. An SSH greeting names only the daemon; naming from it
    ///         produced five cards called "openssh" on a real sweep.
    ///     </para>
    /// </summary>
    private static bool NamesAHost(ServiceIdentity? identity) =>
        identity is { Source: IdentitySource.TlsCertificate };

    private static string Describe(IdentitySource source) => source switch {
        IdentitySource.TlsCertificate => "tls",
        IdentitySource.SshBanner => "ssh",
        IdentitySource.Http => "http",
        _ => "dns"
    };

    /// <summary>
    ///     One MAC answering on several addresses — a gateway's VIPs and aliases — is
    ///     still one machine, so it becomes one card: the lowest address as the card's
    ///     ip (deterministic), every address in an "ips" label. Anything else would make
    ///     the machine's identity depend on how many of its addresses happened to answer
    ///     a particular scan, and identity must never move between scans.
    /// </summary>
    private static IEnumerable<(NetworkHostFact Host, IReadOnlyList<string> AllIps)> Collapse(
        IReadOnlyList<NetworkHostFact> hosts) {
        var byMac = new Dictionary<string, List<NetworkHostFact>>(StringComparer.OrdinalIgnoreCase);

        foreach (NetworkHostFact host in hosts)
            if (host.Mac != null) {
                if (!byMac.TryGetValue(host.Mac, out List<NetworkHostFact>? group))
                    byMac[host.Mac] = group = [];

                group.Add(host);
            }

        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (NetworkHostFact host in hosts) {
            if (host.Mac == null) {
                yield return (host, [host.Ip]);

                continue;
            }

            if (!emitted.Add(host.Mac))
                continue;

            var group = byMac[host.Mac]
                .OrderBy(h => IpHelper.ToUInt32(h.Ip))
                .ToList();

            NetworkHostFact primary = group[0];

            // Any name in the group beats none: a VIP rarely has its own PTR record.
            var hostname = group.Select(h => h.Hostname).FirstOrDefault(n => n != null);

            yield return (
                primary with { Hostname = hostname },
                group.Select(h => h.Ip).ToList());
        }
    }
}
