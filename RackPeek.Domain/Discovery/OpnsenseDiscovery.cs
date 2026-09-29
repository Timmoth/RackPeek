using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Turns a firewall's neighbour table into System resources.
///     <para>
///         The cards are seeded exactly as <see cref="NetworkScanMapper" /> seeds its
///         own — the network scheme, keyed on the MAC — because they describe the same
///         thing by the same evidence: a machine observed on the network rather than
///         asked about itself. That makes the two collectors interchangeable. A host the
///         firewall knows and a host a sweep found are one card, whichever ran first,
///         with no special case anywhere to say so.
///     </para>
/// </summary>
public static class OpnsenseDiscovery {
    public static async Task<List<Resource>> ReadAsync(
        IOpnsenseClient client,
        bool includePublic = false,
        CancellationToken cancellationToken = default) =>
        ToResources(await client.GetNeighboursAsync(cancellationToken), includePublic);

    /// <summary>
    ///     Whether an address belongs to a network someone runs themselves: the RFC 1918
    ///     ranges plus the carrier-grade block an ISP may hand out.
    ///     <para>
    ///         A firewall's WAN leg has neighbours too, and they are the ISP's equipment
    ///         rather than anything the user owns. Recording them would also put a public
    ///         address into a file people commit to git, which is a surprising thing for
    ///         an inventory of a home lab to do on its own. Anyone documenting a fleet on
    ///         public addresses can ask for them.
    ///     </para>
    /// </summary>
    public static bool IsPrivate(string ip) {
        var parts = ip.Split('.');

        if (parts.Length != 4 || !int.TryParse(parts[0], out var a) || !int.TryParse(parts[1], out var b))
            return false;

        return a switch {
            10 => true,
            172 => b is >= 16 and <= 31,
            192 => b == 168,
            100 => b is >= 64 and <= 127, // carrier-grade NAT
            _ => false
        };
    }

    public static List<Resource> ToResources(
        IReadOnlyList<OpnsenseNeighbour> neighbours,
        bool includePublic = false) {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resources = new List<Resource>();

        foreach (OpnsenseNeighbour neighbour in neighbours
                     .Where(n => includePublic || IsPrivate(n.Ip))
                     .OrderBy(n => Order(n.Ip))) {
            var discoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, neighbour.Mac);

            var system = new SystemResource {
                Kind = SystemResource.KindLabel,
                Name = DiscoveryNaming.Unique(
                    DiscoveryNaming.Suggest(
                        DiscoveryNaming.HostLabel(neighbour.Hostname),
                        "host",
                        discoveryId),
                    discoveryId,
                    taken),
                DiscoveryId = discoveryId,
                // Sparse for the same reason a scan's cards are: the firewall knows where
                // a machine is and what its NIC is, never what runs on it. Writing a guess
                // here would overwrite the real values on the next run of a collector that
                // does know.
                Ip = neighbour.Ip
            };

            system.Labels["mac"] = neighbour.Mac;

            // Ours first so the vocabulary matches every other card, the firewall's own
            // lookup second — it carries the whole IEEE registry, so it answers for the
            // prefixes the curated table leaves out.
            var vendor = MacVendorLookup.Lookup(neighbour.Mac) ?? neighbour.Manufacturer;

            // The organisation that owns the NIC's OUI — not a claim about who made the
            // machine, which is a different thing entirely.
            if (vendor != null)
                system.Labels["nic-vendor"] = vendor;

            resources.Add(system);
        }

        return resources;
    }

    private static uint Order(string ip) {
        try {
            return Resources.Services.Networking.IpHelper.ToUInt32(ip);
        }
        catch (ArgumentException) {
            // A malformed address still deserves a card; it just sorts last.
            return uint.MaxValue;
        }
    }
}
