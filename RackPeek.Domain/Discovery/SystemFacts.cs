namespace RackPeek.Domain.Discovery;

/// <summary>
///     The one place discovery turns bytes into the whole gigabytes RackPeek stores,
///     so every collector reports the same size for the same hardware.
/// </summary>
public static class DiscoveryUnits {
    private const double _bytesPerGb = 1024d * 1024 * 1024;

    /// <summary>Rounded, floored at 1 — a real device is never zero gigabytes.</summary>
    public static double BytesToWholeGb(long bytes) => Math.Max(1, Math.Round(bytes / _bytesPerGb));
}

/// <summary>A physical or virtual disk as reported by the host.</summary>
public sealed record BlockDeviceFact(string Name, long SizeBytes, bool Rotational);

/// <summary>A network interface, reduced to the parts that pick a primary address.</summary>
public sealed record NicFact(string Name, bool IsUp, bool IsLoopback, bool HasGateway, string? Ipv4);

/// <summary>
///     Everything a probe managed to read off the host, still in its raw form.
///     Kept deliberately dumb: probes do IO and nothing else, so that every decision
///     made about this data lives in <see cref="SystemFactsParser" /> and is testable
///     on any platform from a captured fixture.
/// </summary>
public sealed record RawSystemSnapshot {
    public string Hostname { get; init; } = string.Empty;
    public int Cores { get; init; }
    public IReadOnlyList<NicFact> Nics { get; init; } = [];
    public IReadOnlyList<BlockDeviceFact> BlockDevices { get; init; } = [];

    /// <summary>Fallback when the platform-specific read fails; always populated.</summary>
    public long FallbackMemoryBytes { get; init; }

    // Linux
    public string? OsReleaseFile { get; init; }
    public string? MemInfoFile { get; init; }
    public string? MachineIdFile { get; init; }
    public string? CgroupFile { get; init; }
    public bool DockerEnvPresent { get; init; }
    public string? DmiVendor { get; init; }
    public string? DmiProduct { get; init; }

    // macOS
    public string? OsName { get; init; }
    public long? MemoryBytes { get; init; }
    public string? PlatformUuid { get; init; }
    public bool HypervisorPresent { get; init; }
}

/// <summary>The host, once the raw snapshot has been interpreted.</summary>
public sealed record SystemFacts {
    public required string Hostname { get; init; }

    /// <summary>Seed for the discovery id. Null when the host offers nothing stable.</summary>
    public string? MachineId { get; init; }

    public required string Os { get; init; }
    public required int Cores { get; init; }
    public required double RamGb { get; init; }

    /// <summary>One of <see cref="Resources.SystemResources.SystemResource.ValidSystemTypes" />.</summary>
    public required string Type { get; init; }

    public string? Ip { get; init; }
    public IReadOnlyList<DriveFact> Drives { get; init; } = [];
}

public sealed record DriveFact(string Type, int SizeGb);
