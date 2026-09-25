
namespace RackPeek.Domain.Discovery;

/// <summary>
///     Turns a <see cref="RawSystemSnapshot" /> into <see cref="SystemFacts" />.
///     Pure: no IO, no platform checks, so it runs and is tested identically everywhere.
/// </summary>
public static class SystemFactsParser {
    /// <summary>Strings that appear in DMI when the host is a guest rather than real hardware.</summary>
    private static readonly string[] _virtualMachineMarkers =
    [
        "qemu", "kvm", "vmware", "virtualbox", "innotek", "xen", "bochs",
        "bhyve", "parallels", "hyper-v", "virtual machine", "openstack"
    ];

    /// <summary>Kernel-managed devices that are not disks anyone wants in an inventory.</summary>
    private static readonly string[] _ignoredBlockDevicePrefixes =
        ["loop", "ram", "zram", "sr", "dm-", "fd", "md"];

    public static SystemFacts Parse(RawSystemSnapshot raw) {
        var type = ParseType(raw);

        return new SystemFacts {
            Hostname = raw.Hostname,
            MachineId = ParseMachineId(raw),
            Os = ParseOs(raw),
            Cores = raw.Cores > 0 ? raw.Cores : 1,
            RamGb = ParseRamGb(raw),
            Type = type,
            Ip = SelectPrimaryIp(raw.Nics),

            // A container sees the host's block devices through /sys/block. They belong
            // to the machine underneath it, so reporting them here would attribute
            // someone else's disks to this resource.
            Drives = type == "container" ? [] : ParseDrives(raw.BlockDevices)
        };
    }

    internal static string? ParseMachineId(RawSystemSnapshot raw) {
        var id = Clean(raw.PlatformUuid) ?? Clean(raw.MachineIdFile);

        return string.IsNullOrEmpty(id) ? null : id;
    }

    internal static string ParseOs(RawSystemSnapshot raw) {
        var name = Clean(raw.OsName);
        if (!string.IsNullOrEmpty(name))
            return name;

        var pretty = ReadKeyValue(raw.OsReleaseFile, "PRETTY_NAME");
        if (!string.IsNullOrEmpty(pretty))
            return pretty;

        var id = ReadKeyValue(raw.OsReleaseFile, "NAME");
        var version = ReadKeyValue(raw.OsReleaseFile, "VERSION");

        if (!string.IsNullOrEmpty(id))
            return string.IsNullOrEmpty(version) ? id : $"{id} {version}";

        return "Unknown";
    }

    internal static double ParseRamGb(RawSystemSnapshot raw) {
        if (raw.MemoryBytes is > 0)
            return DiscoveryUnits.BytesToWholeGb(raw.MemoryBytes.Value);

        // MemTotal is in kB, and is a little under the physical total because the
        // kernel reserves some. Reported as-is rather than rounded up to a DIMM size.
        var memTotal = ReadKeyValue(raw.MemInfoFile, "MemTotal", ':');

        if (!string.IsNullOrEmpty(memTotal)) {
            var digits = new string(memTotal.TakeWhile(char.IsAsciiDigit).ToArray());

            if (long.TryParse(digits, out var kb) && kb > 0)
                return DiscoveryUnits.BytesToWholeGb(kb * 1024L);
        }

        return DiscoveryUnits.BytesToWholeGb(raw.FallbackMemoryBytes);
    }

    internal static string ParseType(RawSystemSnapshot raw) {
        if (raw.DockerEnvPresent || ContainsContainerMarker(raw.CgroupFile))
            return "container";

        if (raw.HypervisorPresent)
            return "vm";

        var dmi = $"{raw.DmiVendor} {raw.DmiProduct}".ToLowerInvariant();

        if (_virtualMachineMarkers.Any(marker => dmi.Contains(marker, StringComparison.Ordinal)))
            return "vm";

        return "baremetal";
    }

    internal static string? SelectPrimaryIp(IReadOnlyList<NicFact> nics) {
        var usable = nics
            .Where(n => n is { IsUp: true, IsLoopback: false } && !string.IsNullOrWhiteSpace(n.Ipv4))
            .ToList();

        // An interface holding the default route is the address other machines reach
        // this host on. Otherwise prefer anything that is not an obvious virtual bridge.
        return usable.FirstOrDefault(n => n.HasGateway)?.Ipv4
               ?? usable.FirstOrDefault(n => !IsVirtual(n.Name))?.Ipv4
               ?? usable.FirstOrDefault()?.Ipv4;
    }

    internal static bool IsVirtual(string name) {
        string[] prefixes = ["docker", "br-", "veth", "virbr", "tailscale", "utun", "tun", "tap", "cni", "flannel"];

        return prefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    internal static List<DriveFact> ParseDrives(IReadOnlyList<BlockDeviceFact> devices) {
        return devices
            .Where(d => !_ignoredBlockDevicePrefixes.Any(p =>
                d.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            .Where(d => d.SizeBytes > 0)
            .Select(d => new DriveFact(DriveType(d), (int)DiscoveryUnits.BytesToWholeGb(d.SizeBytes)))
            .ToList();
    }

    private static string DriveType(BlockDeviceFact device) {
        if (device.Name.StartsWith("nvme", StringComparison.OrdinalIgnoreCase))
            return "nvme";

        if (device.Name.StartsWith("mmcblk", StringComparison.OrdinalIgnoreCase))
            return "sdcard";

        return device.Rotational ? "hdd" : "ssd";
    }

    private static bool ContainsContainerMarker(string? cgroup) {
        if (string.IsNullOrWhiteSpace(cgroup))
            return false;

        string[] markers = ["docker", "lxc", "kubepods", "containerd", "podman"];

        return markers.Any(m => cgroup.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Reads one entry out of a key=value file such as /etc/os-release.</summary>
    private static string? ReadKeyValue(string? contents, string key, char separator = '=') {
        if (string.IsNullOrWhiteSpace(contents))
            return null;

        foreach (var rawLine in contents.Split('\n')) {
            var line = rawLine.Trim();
            var index = line.IndexOf(separator);

            if (index <= 0)
                continue;

            if (!line[..index].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            return line[(index + 1)..].Trim().Trim('"').Trim();
        }

        return null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
