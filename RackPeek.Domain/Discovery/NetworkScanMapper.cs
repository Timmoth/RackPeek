using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Discovery;

/// <summary>Maps swept hosts onto the System resources RackPeek stores. Pure.</summary>
public static class NetworkScanMapper {
    public static List<Resource> ToResources(IReadOnlyList<NetworkHostFact> hosts) {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resources = new List<Resource>(hosts.Count);

        // One MAC answering on several addresses is one box with aliases or VIPs —
        // gateways do this all the time. Each address still gets its own card, but the
        // shared MAC alone cannot identify them: the import rejects duplicate ids.
        var macCounts = hosts
            .Where(h => h.Mac != null)
            .GroupBy(h => h.Mac!)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (NetworkHostFact host in hosts) {
            // The MAC is the only identity a scan can see that survives a DHCP re-lease;
            // when ARP could not provide one (a routed subnet, say) the IP has to do,
            // and the id changes if the address does — documented in the guide.
            var seed = host.Mac == null
                ? $"ip:{host.Ip}"
                : macCounts[host.Mac] > 1
                    ? $"{host.Mac}/{host.Ip}"
                    : host.Mac;

            var discoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, seed);

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

            resources.Add(system);
        }

        return resources;
    }
}
