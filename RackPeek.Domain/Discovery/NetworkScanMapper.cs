using RackPeek.Domain.Resources;
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

            var system = new SystemResource {
                Kind = SystemResource.KindLabel,
                Name = DiscoveryNaming.Unique(
                    DiscoveryNaming.Suggest(
                        DiscoveryNaming.HostLabel(host.Hostname),
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

            if (allIps.Count > 1)
                system.Labels["ips"] = string.Join(",", allIps);

            resources.Add(system);
        }

        return resources;
    }

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
