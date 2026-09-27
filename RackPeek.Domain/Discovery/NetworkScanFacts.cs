using RackPeek.Domain.Resources.Services.Networking;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     One responding host, as the sweep saw it. <see cref="OpenPorts" /> records only
///     what liveness probing happened to touch — port probing stops at the first answer,
///     so this is evidence the host is alive, never a port inventory.
/// </summary>
public sealed record NetworkHostFact(
    string Ip,
    string? Mac,
    string? Hostname,
    bool AnsweredPing,
    IReadOnlyList<int> OpenPorts);

/// <summary>How to sweep. The defaults suit a quiet home /24.</summary>
public sealed record NetworkScanOptions {
    public required Cidr Cidr { get; init; }

    /// <summary>TCP ports probed to catch hosts that do not answer ping.</summary>
    public IReadOnlyList<int> Ports { get; init; } = WellKnownPorts.Defaults;

    public TimeSpan PingTimeout { get; init; } = TimeSpan.FromMilliseconds(300);

    public TimeSpan PortTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>How many hosts are probed at once.</summary>
    public int Concurrency { get; init; } = 128;
}
