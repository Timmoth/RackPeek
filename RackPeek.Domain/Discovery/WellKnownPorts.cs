namespace RackPeek.Domain.Discovery;

/// <summary>
///     The TCP ports the sweep knocks on when a host ignores ping. Chosen for what a
///     homelab actually runs — one open port anywhere in this list is enough to call
///     the host alive, so breadth matters more than depth.
/// </summary>
public static class WellKnownPorts {
    public static readonly IReadOnlyList<int> Defaults = [
        22, // ssh — almost everything
        80, // http
        443, // https
        53, // dns — pi-hole, routers
        445, // smb — nas boxes
        3389, // rdp — windows
        631, // ipp — printers
        8006, // proxmox
        5000, // synology / registries
        8080, // alt http
        8443, // alt https
        9100 // node-exporter / jetdirect
    ];
}
