using RackPeek.Domain.Resources.Services.Networking;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Network IO for the sweep. The IO half of network discovery, mirroring
///     <see cref="IDockerClient" /> / <see cref="IProxmoxClient" />: everything here is
///     untestable-by-design plumbing, and every decision made about what comes back
///     lives in <see cref="NetworkScanner" /> and the pure parsers.
/// </summary>
public interface INetworkProbe {
    /// <summary>True when the host answers an ICMP echo within the timeout.</summary>
    Task<bool> PingAsync(string ip, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>True when a TCP connect to the port completes within the timeout.</summary>
    Task<bool> TryConnectAsync(string ip, int port, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    ///     The host's ARP table, raw, in whichever format this platform produces — the
    ///     sweep's pings populate it, and <see cref="ArpTableParser" /> reads either
    ///     format. Null when it cannot be read; MACs are an enrichment, not a requirement.
    /// </summary>
    Task<string?> ReadArpAsync(CancellationToken cancellationToken = default);

    /// <summary>The host's reverse-DNS name, or null when it has none worth keeping.</summary>
    Task<string?> ReverseDnsAsync(string ip, CancellationToken cancellationToken = default);

    /// <summary>
    ///     The subnet of the first up, non-loopback IPv4 interface with a gateway — what
    ///     `--cidr` defaults to. Null when the machine has no such interface.
    /// </summary>
    Cidr? LocalSubnet();
}
