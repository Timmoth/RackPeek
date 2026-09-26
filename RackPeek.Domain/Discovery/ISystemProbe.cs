namespace RackPeek.Domain.Discovery;

/// <summary>
///     Reads raw facts off the host. The only platform-specific code in discovery, and
///     deliberately the only part that is not unit tested — it does IO and nothing else.
/// </summary>
public interface ISystemProbe {
    /// <summary>True when this probe can run on the current host.</summary>
    bool IsSupported { get; }

    Task<RawSystemSnapshot> ReadAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Probe selection, shared by every collector so they all describe the host — and
///     seed discovery ids — identically.
/// </summary>
public static class SystemProbes {
    /// <summary>Facts from the first supported probe, or null on an unsupported platform.</summary>
    public static async Task<SystemFacts?> TryReadHostAsync(
        IEnumerable<ISystemProbe> probes,
        CancellationToken cancellationToken) {
        ISystemProbe? probe = probes.FirstOrDefault(p => p.IsSupported);

        if (probe == null)
            return null;

        return SystemFactsParser.Parse(await probe.ReadAsync(cancellationToken));
    }
}
