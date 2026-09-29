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
    IReadOnlyList<int> OpenPorts) {
    /// <summary>
    ///     A name a service volunteered — a TLS certificate's CN, an SSH greeting, an
    ///     HTTP title. Null when nothing answered or nothing said anything useful. Never
    ///     overrides <see cref="Hostname" />: a PTR record is the network's own answer.
    /// </summary>
    public ServiceIdentity? Identity { get; init; }

    /// <summary>
    ///     Applications that named themselves over HTTP, one per port. These describe
    ///     what the host runs rather than what it is, so they become Service resources
    ///     rather than deciding the host's name.
    /// </summary>
    public IReadOnlyList<ServiceIdentity> Services { get; init; } = [];

    /// <summary>
    ///     The organisation IEEE assigned <see cref="Mac" />'s OUI to, or a note that the
    ///     address is self-assigned. Null when there is no MAC, or none is known for it.
    /// </summary>
    public string? Vendor { get; init; }
}

/// <summary>How to sweep. The defaults suit a quiet home /24.</summary>
public sealed record NetworkScanOptions {
    public required Cidr Cidr { get; init; }

    /// <summary>TCP ports probed to catch hosts that do not answer ping.</summary>
    public IReadOnlyList<int> Ports { get; init; } = WellKnownPorts.Defaults;

    public TimeSpan PingTimeout { get; init; } = TimeSpan.FromMilliseconds(300);

    public TimeSpan PortTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Cap on each alive host's reverse-DNS lookup — resolvers that silently
    /// drop PTR queries would otherwise stall the whole result on the OS default.</summary>
    public TimeSpan DnsTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>How many hosts are probed at once.</summary>
    public int Concurrency { get; init; } = 128;

    /// <summary>
    ///     Whether living hosts are asked what they are — a TLS certificate CN, an SSH
    ///     greeting, an HTTP title. Costs a handful of short connections per host and is
    ///     the difference between "host-1a2b3c4d" and "pve-node-01". Off makes the sweep a
    ///     pure liveness check again.
    /// </summary>
    public bool IdentifyServices { get; init; } = true;

    /// <summary>Cap on each identity probe; these run per living host, not per address.</summary>
    public TimeSpan IdentifyTimeout { get; init; } = TimeSpan.FromMilliseconds(1200);

    /// <summary>
    ///     Ports a living host is checked against, over and above <see cref="Ports" />.
    ///     Wider because it is paid per living host rather than per address.
    /// </summary>
    public IReadOnlyList<int> IdentityPorts { get; init; } = WellKnownPorts.Identity;
}
