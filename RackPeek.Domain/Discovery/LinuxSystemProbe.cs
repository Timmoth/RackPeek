namespace RackPeek.Domain.Discovery;

public sealed class LinuxSystemProbe : ISystemProbe {
    private const string _blockDeviceRoot = "/sys/block";
    private const long _sectorBytes = 512;

    public bool IsSupported => OperatingSystem.IsLinux();

    public async Task<RawSystemSnapshot> ReadAsync(CancellationToken cancellationToken = default) {
        return new RawSystemSnapshot {
            Hostname = SystemProbeCommon.Hostname(),
            Cores = SystemProbeCommon.Cores(),
            Nics = SystemProbeCommon.Nics(),
            FallbackMemoryBytes = SystemProbeCommon.FallbackMemoryBytes(),
            BlockDevices = ReadBlockDevices(),
            OsReleaseFile = await SystemProbeCommon.TryReadFileAsync("/etc/os-release", cancellationToken),
            MemInfoFile = await SystemProbeCommon.TryReadFileAsync("/proc/meminfo", cancellationToken),
            MachineIdFile = await ReadMachineIdAsync(cancellationToken),
            CgroupFile = await SystemProbeCommon.TryReadFileAsync("/proc/1/cgroup", cancellationToken),
            DockerEnvPresent = File.Exists("/.dockerenv"),
            DmiVendor = await SystemProbeCommon.TryReadFileAsync("/sys/class/dmi/id/sys_vendor", cancellationToken),
            DmiProduct = await SystemProbeCommon.TryReadFileAsync("/sys/class/dmi/id/product_name", cancellationToken)
        };
    }

    private static async Task<string?> ReadMachineIdAsync(CancellationToken cancellationToken) =>
        await SystemProbeCommon.TryReadFileAsync("/etc/machine-id", cancellationToken)
        ?? await SystemProbeCommon.TryReadFileAsync("/var/lib/dbus/machine-id", cancellationToken);

    /// <summary>
    ///     Whole disks as the kernel sees them, which is closer to what goes on an
    ///     inventory card than the mounted filesystems would be.
    /// </summary>
    private static List<BlockDeviceFact> ReadBlockDevices() {
        try {
            if (!Directory.Exists(_blockDeviceRoot))
                return [];

            return Directory.EnumerateDirectories(_blockDeviceRoot)
                .Select(ReadBlockDevice)
                .OfType<BlockDeviceFact>()
                .OrderBy(d => d.Name, StringComparer.Ordinal)
                .ToList();
        }
        catch {
            return [];
        }
    }

    private static BlockDeviceFact? ReadBlockDevice(string path) {
        try {
            var name = Path.GetFileName(path);

            if (!long.TryParse(File.ReadAllText(Path.Combine(path, "size")).Trim(), out var sectors))
                return null;

            var rotationalPath = Path.Combine(path, "queue", "rotational");
            var rotational = File.Exists(rotationalPath)
                             && File.ReadAllText(rotationalPath).Trim() == "1";

            return new BlockDeviceFact(name, sectors * _sectorBytes, rotational);
        }
        catch {
            return null;
        }
    }
}
