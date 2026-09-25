namespace RackPeek.Domain.Discovery;

/// <summary>Reads a Proxmox VE API. The IO half of hypervisor discovery.</summary>
public interface IProxmoxClient {
    /// <summary>Where this client is pointed, for error messages.</summary>
    string Endpoint { get; }

    /// <summary>
    ///     The scope a vmid is unique within — the cluster name where there is one, and
    ///     the node otherwise. Part of every guest's identity, so a guest that migrates
    ///     between nodes stays the same resource.
    /// </summary>
    Task<string> GetIdentityScopeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProxmoxNode>> GetNodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds what only the per-node status call knows — currently the Proxmox version.
    ///     Returns the node unchanged if the token may not read it: a read-only token
    ///     without Sys.Audit can still see the guests, and a partial answer beats none.
    /// </summary>
    Task<ProxmoxNode> EnrichAsync(ProxmoxNode node, CancellationToken cancellationToken = default);

    /// <summary>Guests of one kind on one node. <paramref name="endpoint" /> is qemu or lxc.</summary>
    Task<IReadOnlyList<ProxmoxGuest>> GetGuestsAsync(
        string node,
        string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Physical disks on a node. Needs the same permission as the status call, so it
    ///     is gathered as part of enrichment and simply absent without it.
    /// </summary>
    Task<IReadOnlyList<ProxmoxDisk>> GetDisksAsync(string node, CancellationToken cancellationToken = default);

    /// <summary>Display adapters in a node, from its PCI device list.</summary>
    Task<IReadOnlyList<ProxmoxGpu>> GetGpusAsync(string node, CancellationToken cancellationToken = default);

    Task<ProxmoxGuestConfig> GetGuestConfigAsync(
        string node,
        string endpoint,
        int vmId,
        CancellationToken cancellationToken = default);
}
