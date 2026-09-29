namespace RackPeek.Domain.Discovery;

/// <summary>
///     macOS host probe. Disks are deliberately left out: everything that reports them
///     (diskutil, system_profiler) needs a plist parser for a field that is optional
///     anyway, and omitting is better than guessing — null means "don't touch" on merge.
/// </summary>
public sealed class MacSystemProbe : ISystemProbe {
    public bool IsSupported => OperatingSystem.IsMacOS();

    public async Task<RawSystemSnapshot> ReadAsync(CancellationToken cancellationToken = default) {
        // The four probes spawn independent processes, so they run side by side.
        Task<string?> osName = ReadOsNameAsync(cancellationToken);
        Task<long?> memoryBytes = ReadMemoryBytesAsync(cancellationToken);
        Task<string?> platformUuid = ReadPlatformUuidAsync(cancellationToken);
        Task<bool> hypervisorPresent = ReadHypervisorPresentAsync(cancellationToken);

        return new RawSystemSnapshot {
            Hostname = SystemProbeCommon.Hostname(),
            Cores = SystemProbeCommon.Cores(),
            Nics = SystemProbeCommon.Nics(),
            FallbackMemoryBytes = SystemProbeCommon.FallbackMemoryBytes(),
            OsName = await osName,
            MemoryBytes = await memoryBytes,
            PlatformUuid = await platformUuid,
            HypervisorPresent = await hypervisorPresent
        };
    }

    private static async Task<string?> ReadOsNameAsync(CancellationToken cancellationToken) {
        var product = await SystemProbeCommon.TryRunAsync("sw_vers", "-productName", cancellationToken);
        var version = await SystemProbeCommon.TryRunAsync("sw_vers", "-productVersion", cancellationToken);

        if (string.IsNullOrWhiteSpace(product))
            return null;

        return string.IsNullOrWhiteSpace(version) ? product : $"{product} {version}";
    }

    private static async Task<long?> ReadMemoryBytesAsync(CancellationToken cancellationToken) {
        var value = await SystemProbeCommon.TryRunAsync("sysctl", "-n hw.memsize", cancellationToken);

        return long.TryParse(value, out var bytes) ? bytes : null;
    }

    private static async Task<bool> ReadHypervisorPresentAsync(CancellationToken cancellationToken) {
        var value = await SystemProbeCommon.TryRunAsync("sysctl", "-n kern.hv_vmm_present", cancellationToken);

        return value?.Trim() == "1";
    }

    private static async Task<string?> ReadPlatformUuidAsync(CancellationToken cancellationToken) {
        var output = await SystemProbeCommon.TryRunAsync(
            "ioreg", "-rd1 -c IOPlatformExpertDevice", cancellationToken);

        return ParsePlatformUuid(output);
    }

    /// <summary>Pulls IOPlatformUUID out of an ioreg dump. Public so it can be tested off a macOS host.</summary>
    public static string? ParsePlatformUuid(string? ioregOutput) {
        if (string.IsNullOrWhiteSpace(ioregOutput))
            return null;

        foreach (var line in ioregOutput.Split('\n')) {
            if (!line.Contains("IOPlatformUUID", StringComparison.Ordinal))
                continue;

            var parts = line.Split('=', 2);

            if (parts.Length == 2)
                return parts[1].Trim().Trim('"').Trim();
        }

        return null;
    }
}
