using RackPeek.Domain.Resources;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Reads a whole Proxmox endpoint and maps it to resources.
///     <para>
///         Lives here rather than in a command so the CLI and the MCP tool run the same
///         code. They used to hold a copy each, and the copies had already drifted — the
///         MCP one dropped the guests' MACs, which silently cost every guest its chance
///         of unifying with a network scan.
///     </para>
/// </summary>
public static class ProxmoxDiscovery {
    public static async Task<List<Resource>> ReadAsync(
        IProxmoxClient client,
        CancellationToken cancellationToken = default) {
        var scope = await client.GetIdentityScopeAsync(cancellationToken);
        IReadOnlyList<ProxmoxNode> listed = await client.GetNodesAsync(cancellationToken);

        var nodes = new List<ProxmoxNode>();
        var guests = new List<ProxmoxGuest>();

        foreach (ProxmoxNode listedNode in listed) {
            // Node detail needs a broader permission than listing guests does, so it is
            // enrichment rather than a requirement — a read-only token still gets a tree.
            ProxmoxNode node = await client.EnrichAsync(listedNode, cancellationToken);
            nodes.Add(node);

            var nodeName = node.Name;

            foreach (var endpoint in new[] { ProxmoxApiClient.QemuEndpoint, ProxmoxApiClient.LxcEndpoint }) {
                IReadOnlyList<ProxmoxGuest> listedGuests =
                    await client.GetGuestsAsync(nodeName, endpoint, cancellationToken);

                // The list call knows nothing about the OS, and for a container it does
                // not know the address either. Both live in the guest's own config — one
                // call per guest, so they run concurrently rather than one at a time.
                ProxmoxGuestConfig[] configs = await Task.WhenAll(listedGuests.Select(g =>
                    client.GetGuestConfigAsync(nodeName, endpoint, g.VmId, cancellationToken)));

                // Ask the running guests where they are. The config only carries an
                // address when someone set one statically, so on a DHCP estate this is
                // the difference between every guest having an address and none of them
                // having one — and an address is what lets a guest line up with the host
                // a network sweep found at that address.
                IReadOnlyList<ProxmoxGuestAddress>[] addresses = await Task.WhenAll(
                    listedGuests.Select(g => IsRunning(g)
                        ? client.GetGuestAddressesAsync(nodeName, endpoint, g.VmId, cancellationToken)
                        : Task.FromResult<IReadOnlyList<ProxmoxGuestAddress>>([])));

                for (var i = 0; i < listedGuests.Count; i++) {
                    IReadOnlyList<string> macs = configs[i].Macs ?? [];

                    guests.Add(listedGuests[i] with {
                        Os = configs[i].Os,
                        Ip = configs[i].Ip ?? SelectGuestIp(addresses[i], macs),
                        Disks = configs[i].DiskBytes,
                        PassthroughAddresses = configs[i].PassthroughAddresses,
                        Macs = macs
                    });
                }
            }
        }

        return ProxmoxResourceMapper.ToResources(scope, nodes, guests);
    }

    /// <summary>
    ///     The address that belongs to the guest itself.
    ///     <para>
    ///         A guest agent reports every interface inside the machine, and a guest that
    ///         runs containers has several: Docker's <c>docker0</c> and its per-network
    ///         bridges, Home Assistant's <c>hassio</c>, any VPN tunnel. Recording
    ///         172.17.0.1 as the machine's address would be worse than recording nothing,
    ///         because every Docker host on the estate reports the same one.
    ///     </para>
    ///     <para>
    ///         The NIC MACs Proxmox assigned are the discriminator: they are already read
    ///         from the guest's config, and an interface carrying one is a NIC the
    ///         hypervisor gave the guest rather than something the guest invented. When
    ///         the MACs are unknown — a container, or a config the token cannot read —
    ///         nothing is claimed, since a guess here is indistinguishable from a fact.
    ///     </para>
    /// </summary>
    public static string? SelectGuestIp(
        IReadOnlyList<ProxmoxGuestAddress> addresses,
        IReadOnlyList<string> configuredMacs) {
        if (addresses.Count == 0 || configuredMacs.Count == 0)
            return null;

        var allowed = new HashSet<string>(
            configuredMacs.Select(ArpTableParser.NormaliseMac).Where(m => m != null)!,
            StringComparer.OrdinalIgnoreCase);

        if (allowed.Count == 0)
            return null;

        return addresses
            .Where(a => ArpTableParser.NormaliseMac(a.Mac) is { } mac && allowed.Contains(mac))
            .Select(a => a.Ip)
            .FirstOrDefault();
    }

    private static bool IsRunning(ProxmoxGuest guest) =>
        string.Equals(guest.Status, "running", StringComparison.OrdinalIgnoreCase);
}
