using System.Text.Json;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     A Proxmox node: the physical machine and the hypervisor installed on it. RackPeek
///     models those as two resources, so both sets of facts are gathered here.
/// </summary>
public sealed record ProxmoxNode {
    public required string Name { get; init; }

    /// <summary>Logical processors, which is what the hypervisor OS sees.</summary>
    public int Cores { get; init; }

    public long MemoryBytes { get; init; }

    /// <summary>e.g. <c>pve-manager/8.2.2/9355359cd7afbae4</c>, only from the status call.</summary>
    public string? Version { get; init; }

    // Hardware, all from the status call and all optional — a token without Sys.Audit
    // still gets a usable node, just without these.
    public string? CpuModel { get; init; }
    public int Sockets { get; init; }
    public int PhysicalCores { get; init; }
    public IReadOnlyList<ProxmoxDisk> Disks { get; init; } = [];

    /// <summary>
    ///     Display adapters in the machine. A GPU passed through to a guest is still
    ///     physically in the host, so this is where it belongs — the PCI address is kept
    ///     so a guest holding it can say which card it has.
    /// </summary>
    public IReadOnlyList<ProxmoxGpu> Gpus { get; init; } = [];
}

/// <summary>A physical disk as Proxmox reports it, already classified by type.</summary>
public sealed record ProxmoxDisk(string Type, long SizeBytes, string? Model);

/// <summary>A display adapter and where it sits on the bus.</summary>
public sealed record ProxmoxGpu(string Address, string Model);

/// <summary>A guest on a node. QEMU and LXC differ only in the kind of system they are.</summary>
public sealed record ProxmoxGuest {
    public required int VmId { get; init; }
    public required string Node { get; init; }

    /// <summary>Empty for a guest that has never been named; the mapper falls back to the vmid.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary><c>vm</c> or <c>container</c>, matching SystemResource.ValidSystemTypes.</summary>
    public required string Type { get; init; }

    public int Cores { get; init; }
    public long MemoryBytes { get; init; }
    /// <summary>The boot disk, from the guest list. A fallback for when the config is unreadable.</summary>
    public long DiskBytes { get; init; }

    /// <summary>Every attached disk, from the guest's config.</summary>
    public IReadOnlyList<long> Disks { get; init; } = [];

    /// <summary>PCI addresses handed exclusively to this guest, from its config.</summary>
    public IReadOnlyList<string> PassthroughAddresses { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Filled in from the guest's config, which is the only place it is known.</summary>
    public string? Os { get; init; }

    public string? Ip { get; init; }
}

/// <summary>
///     Parses the Proxmox API. Every response wraps its payload in a <c>data</c> member.
///     Pure, so it tests from captured responses without a Proxmox to talk to.
/// </summary>
public static class ProxmoxResponseParser {
    public const string VmType = "vm";
    public const string ContainerType = "container";

    /// <summary>
    ///     Nodes with whatever detail the token is allowed to see. Proxmox strips
    ///     <c>maxcpu</c> and <c>maxmem</c> from this response for a token without the
    ///     rights to read them, rather than refusing the call, so both shapes are normal.
    /// </summary>
    public static List<ProxmoxNode> ParseNodes(string json) {
        return Data(json)
            .Where(n => !string.IsNullOrWhiteSpace(GetString(n, "node")))
            .Select(n => new ProxmoxNode {
                Name = GetString(n, "node")!,
                Cores = GetInt(n, "maxcpu") ?? 0,
                MemoryBytes = GetLong(n, "maxmem") ?? 0
            })
            .OrderBy(n => n.Name, StringComparer.Ordinal)
            .ToList();
    }

    public static ProxmoxNode ParseNodeStatus(string json, string nodeName) {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("data", out JsonElement data))
            return new ProxmoxNode { Name = nodeName };

        var cores = data.TryGetProperty("cpuinfo", out JsonElement cpu)
            ? GetInt(cpu, "cpus") ?? GetInt(cpu, "cores") ?? 0
            : 0;

        var memory = data.TryGetProperty("memory", out JsonElement mem)
            ? GetLong(mem, "total") ?? 0
            : 0;

        return new ProxmoxNode {
            Name = nodeName,
            Cores = cores,
            MemoryBytes = memory,
            Version = GetString(data, "pveversion"),
            CpuModel = cpu.ValueKind == JsonValueKind.Object ? GetString(cpu, "model") : null,
            Sockets = cpu.ValueKind == JsonValueKind.Object ? GetInt(cpu, "sockets") ?? 0 : 0,
            PhysicalCores = cpu.ValueKind == JsonValueKind.Object ? GetInt(cpu, "cores") ?? 0 : 0
        };
    }

    /// <summary>
    ///     Physical disks. Proxmox has already worked out nvme/ssd/hdd, which is better
    ///     than the guess <c>discover system</c> has to make from a rotational flag.
    /// </summary>
    public static List<ProxmoxDisk> ParseDisks(string json) {
        return Data(json)
            .Select(d => new ProxmoxDisk(
                NormaliseDiskType(GetString(d, "type")),
                GetLong(d, "size") ?? 0,
                GetString(d, "model")))
            .Where(d => d.SizeBytes > 0)
            .ToList();
    }

    /// <summary>Proxmox says "unknown" for a disk it cannot classify; RackPeek omits the type.</summary>
    private static string NormaliseDiskType(string? type) {
        return type?.ToLowerInvariant() switch {
            "nvme" => "nvme",
            "ssd" => "ssd",
            "hdd" => "hdd",
            _ => string.Empty
        };
    }

    /// <summary>
    ///     Display adapters from the node's PCI device list. PCI class 0x03 is the
    ///     display-controller class, which is how a GPU is told apart from the other
    ///     couple of dozen devices on a modern board.
    /// </summary>
    public static List<ProxmoxGpu> ParseGpus(string json) {
        return Data(json)
            .Where(d => (GetString(d, "class") ?? string.Empty).StartsWith("0x03", StringComparison.Ordinal))
            .Select(d => new { Address = GetString(d, "id"), Model = MarketingName(GetString(d, "device_name")) })
            .Where(g => !string.IsNullOrWhiteSpace(g.Address) && !string.IsNullOrWhiteSpace(g.Model))
            .Select(g => new ProxmoxGpu(g.Address!, g.Model!))
            .ToList();
    }

    /// <summary>
    ///     PCI addresses a guest has been given exclusive use of. The config writes them
    ///     as <c>hostpci0: 0000:01:00</c>, optionally with trailing options and sometimes
    ///     without the function suffix the device list carries.
    /// </summary>
    public static List<string> ParsePassthrough(JsonElement config) {
        return config.EnumerateObject()
            .Where(p => p.Name.StartsWith("hostpci", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : null)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Split(',')[0].Trim())
            .Where(v => v.Length > 0)
            .ToList();
    }

    /// <summary>
    ///     PCI names the part and then the product: <c>GA102 [GeForce RTX 3090]</c>. The
    ///     bracketed half is the one people would have typed, so it wins where it exists.
    /// </summary>
    public static string? MarketingName(string? deviceName) {
        if (string.IsNullOrWhiteSpace(deviceName))
            return null;

        var open = deviceName.IndexOf('[');
        var close = deviceName.IndexOf(']');

        return close > open && open >= 0
            ? deviceName[(open + 1)..close].Trim()
            : deviceName.Trim();
    }

    public static List<ProxmoxGuest> ParseGuests(string json, string node, string type) {
        return Data(json)
            .Select(g => ParseGuest(g, node, type))
            .OfType<ProxmoxGuest>()
            .OrderBy(g => g.VmId)
            .ToList();
    }

    /// <summary>
    ///     The scope a vmid is unique within. A clustered guest can migrate between nodes,
    ///     so the cluster name is what keeps its identity stable; a standalone host has no
    ///     cluster entry and falls back to the node.
    /// </summary>
    public static string ParseIdentityScope(string clusterStatusJson, string fallbackNode) {
        JsonElement cluster = Data(clusterStatusJson)
            .FirstOrDefault(e => GetString(e, "type") == "cluster");

        var name = cluster.ValueKind == JsonValueKind.Object ? GetString(cluster, "name") : null;

        return string.IsNullOrWhiteSpace(name) ? fallbackNode : name;
    }

    /// <summary>
    ///     Reads the guest's own config. This is the only place the OS is knowable, and
    ///     for a container it carries the address too — which is why the extra call per
    ///     guest earns its place.
    /// </summary>
    public static ProxmoxGuestConfig ParseGuestConfig(string json) {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("data", out JsonElement data))
            return new ProxmoxGuestConfig(null, null, [], []);

        return new ProxmoxGuestConfig(
            DescribeOs(GetString(data, "ostype")),
            ParseStaticIp(GetString(data, "net0")),
            ParseDiskSizes(data),
            ParsePassthrough(data));
    }

    /// <summary>
    ///     Every disk attached to a guest. The guest list only carries <c>maxdisk</c>,
    ///     which is the boot disk alone — a VM with a small root and a large data volume
    ///     would otherwise be recorded at a fraction of its real size.
    /// </summary>
    public static List<long> ParseDiskSizes(JsonElement config) {
        var sizes = new List<long>();

        foreach (JsonProperty property in config.EnumerateObject()) {
            if (!IsDiskSlot(property.Name))
                continue;

            var value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;

            if (value == null || value.Contains("media=cdrom", StringComparison.OrdinalIgnoreCase))
                continue;

            var size = ParseSize(value);

            if (size > 0)
                sizes.Add(size);
        }

        return sizes;
    }

    /// <summary>
    ///     Disk-bearing config keys. <c>unusedN</c> is excluded because it is a detached
    ///     volume with no size, and the EFI and TPM state volumes because they are
    ///     firmware scratch space of a few megabytes rather than storage anyone inventories.
    /// </summary>
    private static bool IsDiskSlot(string key) {
        string[] prefixes = ["scsi", "virtio", "sata", "ide", "mp"];

        if (key.Equals("rootfs", StringComparison.OrdinalIgnoreCase))
            return true;

        return prefixes.Any(p =>
            key.StartsWith(p, StringComparison.OrdinalIgnoreCase)
            && key.Length > p.Length
            && key[p.Length..].All(char.IsAsciiDigit));
    }

    /// <summary>
    ///     Reads <c>size=64G</c> out of a volume definition, in bytes. Proxmox permits a
    ///     fractional number (<c>size=4.5G</c>, after an odd resize) and a bare number,
    ///     which is bytes.
    /// </summary>
    public static long ParseSize(string volume) {
        foreach (var part in volume.Split(',', StringSplitOptions.TrimEntries)) {
            if (!part.StartsWith("size=", StringComparison.OrdinalIgnoreCase))
                continue;

            var raw = part[5..].Trim();

            if (raw.Length == 0)
                return 0;

            var multiplier = char.ToUpperInvariant(raw[^1]) switch {
                'K' => 1024L,
                'M' => 1024L * 1024,
                'G' => 1024L * 1024 * 1024,
                'T' => 1024L * 1024 * 1024 * 1024,
                _ => 0L
            };

            if (multiplier == 0)
                return long.TryParse(raw, out var bytes) && bytes > 0 ? bytes : 0;

            return double.TryParse(
                       raw[..^1],
                       System.Globalization.NumberStyles.Float,
                       System.Globalization.CultureInfo.InvariantCulture,
                       out var value)
                   && value > 0
                ? (long)Math.Round(value * multiplier)
                : 0;
        }

        return 0;
    }

    /// <summary>
    ///     Proxmox stores an ostype code. The container ones name a real distribution and
    ///     are worth having; the QEMU ones are coarse by nature — <c>l26</c> means any
    ///     Linux since 2.6 — so they stay vague rather than pretending to precision.
    /// </summary>
    internal static string? DescribeOs(string? ostype) {
        if (string.IsNullOrWhiteSpace(ostype))
            return null;

        return ostype.ToLowerInvariant() switch {
            "l24" => "Linux",
            "l26" => "Linux",
            "solaris" => "Solaris",
            "wxp" => "Windows XP",
            "w2k" => "Windows 2000",
            "w2k3" => "Windows Server 2003",
            "w2k8" => "Windows Server 2008",
            "wvista" => "Windows Vista",
            "win7" => "Windows 7",
            "win8" => "Windows 8",
            "win10" => "Windows 10",
            "win11" => "Windows 11",
            "other" => null,
            "unmanaged" => null,
            // Container templates are named after the distribution itself.
            var distribution => char.ToUpperInvariant(distribution[0]) + distribution[1..]
        };
    }

    /// <summary>
    ///     Pulls the address out of a net interface line such as
    ///     <c>name=eth0,bridge=vmbr0,ip=192.168.1.53/24</c>. Returns null for
    ///     <c>ip=dhcp</c> and <c>ip=manual</c>, where the config knows no more than we do.
    /// </summary>
    internal static string? ParseStaticIp(string? net) {
        if (string.IsNullOrWhiteSpace(net))
            return null;

        foreach (var part in net.Split(',', StringSplitOptions.TrimEntries)) {
            if (!part.StartsWith("ip=", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = part[3..].Split('/')[0].Trim();

            return value.Equals("dhcp", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("manual", StringComparison.OrdinalIgnoreCase)
                   || value.Length == 0
                ? null
                : value;
        }

        return null;
    }

    private static ProxmoxGuest? ParseGuest(JsonElement element, string node, string type) {
        var vmid = GetInt(element, "vmid");

        if (vmid == null)
            return null;

        return new ProxmoxGuest {
            VmId = vmid.Value,
            Node = node,
            Name = GetString(element, "name") ?? string.Empty,
            Type = type,
            Cores = GetInt(element, "cpus") ?? 0,
            MemoryBytes = GetLong(element, "maxmem") ?? 0,
            DiskBytes = GetLong(element, "maxdisk") ?? 0,
            Tags = ParseTags(GetString(element, "tags"))
        };
    }

    /// <summary>Proxmox joins guest tags with semicolons.</summary>
    private static List<string> ParseTags(string? tags) {
        if (string.IsNullOrWhiteSpace(tags))
            return [];

        return tags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(DiscoveryNaming.Slug)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<JsonElement> Data(string json) {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("data", out JsonElement data)
            || data.ValueKind != JsonValueKind.Array)
            return [];

        return data.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? GetInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.TryGetInt32(out var result)
            ? result
            : null;

    private static long? GetLong(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.TryGetInt64(out var result)
            ? result
            : null;
}

/// <summary>The parts of a guest's config worth recording. Everything is optional.</summary>
public sealed record ProxmoxGuestConfig(
    string? Os,
    string? Ip,
    IReadOnlyList<long> DiskBytes,
    IReadOnlyList<string> PassthroughAddresses);
