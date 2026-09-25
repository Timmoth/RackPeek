using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Maps a Proxmox estate onto RackPeek's three levels. Pure.
///     <para>
///         A node becomes two resources, because it is two things: the machine
///         (<c>kepler</c>, a Server carrying the CPU, memory and disks) and the
///         hypervisor installed on it (<c>kepler-pve</c>, a System). Guests then run on
///         the hypervisor, giving Hardware -> System -> System all the way down.
///     </para>
/// </summary>
public static class ProxmoxResourceMapper {
    public const string Scheme = "pve";

    /// <summary>Label recording which cards a guest has exclusive use of.</summary>
    public const string GpuLabel = "gpu";

    /// <summary>The cap RackPeek's own validation puts on a label value.</summary>
    private const int _maxLabelLength = Helpers.ThrowIfInvalid.MaxLabelValueLength;

    public static List<Resource> ToResources(
        string scope,
        IReadOnlyList<ProxmoxNode> nodes,
        IReadOnlyList<ProxmoxGuest> guests) {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resources = new List<Resource>();

        // Nodes first, so a guest named after its node does not take the node's name.
        var hypervisorNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // PCI addresses repeat on every machine, so a guest can only be matched against
        // the cards in the node it actually runs on.
        var gpusByNode = nodes.ToDictionary(
            n => n.Name,
            n => n.Gpus,
            StringComparer.OrdinalIgnoreCase);

        foreach (ProxmoxNode node in nodes) {
            Server server = ToServer(node, scope, taken);
            SystemResource hypervisor = ToHypervisor(node, scope, server.Name, taken);

            hypervisorNames[node.Name] = hypervisor.Name;
            resources.Add(server);
            resources.Add(hypervisor);
        }

        // A vmid is unique within the scope, so two entries carrying the same one are
        // the same guest — which is what a migration in flight looks like, reported by
        // both the node it is leaving and the node it is joining. Without this they
        // would come out as two resources sharing an identity.
        resources.AddRange(guests
            .DistinctBy(g => g.VmId)
            .Select(g => ToResource(g, scope, hypervisorNames, gpusByNode, taken)));

        return resources;
    }

    /// <summary>The machine itself. Takes the node's own name, being the thing people point at.</summary>
    private static Server ToServer(ProxmoxNode node, string scope, ISet<string> taken) {
        var discoveryId = DiscoveryId.Create(Scheme, $"{scope}/node/{node.Name}");

        return new Server {
            Kind = Server.KindLabel,
            Name = DiscoveryNaming.Unique(
                DiscoveryNaming.Suggest(node.Name, "server", discoveryId),
                discoveryId,
                taken),
            DiscoveryId = discoveryId,
            Ram = ToGb(node.MemoryBytes) is { } ram ? new Ram { Size = ram } : null,
            Cpus = ToCpus(node),
            Drives = ToDrives(node.Disks),

            // A GPU passed through to a guest is still bolted into this machine, so it
            // is recorded here rather than on whatever borrows it. VRAM is not something
            // the PCI list knows, so it is left off.
            Gpus = node.Gpus.Count == 0 ? null : node.Gpus.Select(g => new Gpu { Model = g.Model }).ToList()
        };
    }

    /// <summary>The Proxmox install running on the machine, which is what guests run on.</summary>
    private static SystemResource ToHypervisor(
        ProxmoxNode node,
        string scope,
        string serverName,
        ISet<string> taken) {
        var discoveryId = DiscoveryId.Create(Scheme, $"{scope}/node/{node.Name}/pve");

        return new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = DiscoveryNaming.Unique(
                DiscoveryNaming.Suggest($"{node.Name}-pve", "hypervisor", discoveryId),
                discoveryId,
                taken),
            DiscoveryId = discoveryId,
            Type = "hypervisor",
            Os = DescribeVersion(node.Version),
            Cores = node.Cores > 0 ? node.Cores : null,
            Ram = ToGb(node.MemoryBytes),
            RunsOn = [serverName]
        };
    }

    /// <summary>
    ///     One entry per socket. Proxmox reports totals across the machine, so they are
    ///     divided down — two sockets of an 8-core part read as 2 x 8, not 1 x 16.
    /// </summary>
    private static List<Cpu>? ToCpus(ProxmoxNode node) {
        if (string.IsNullOrWhiteSpace(node.CpuModel))
            return null;

        var sockets = Math.Max(1, node.Sockets);

        var cpu = new Cpu {
            Model = node.CpuModel,
            Cores = node.PhysicalCores > 0 ? node.PhysicalCores / sockets : null,
            Threads = node.Cores > 0 ? node.Cores / sockets : null
        };

        return Enumerable.Range(0, sockets).Select(_ => cpu).ToList();
    }

    private static List<Drive>? ToDrives(IReadOnlyList<ProxmoxDisk> disks) {
        if (disks.Count == 0)
            return null;

        return disks
            .Select(d => new Drive {
                Type = string.IsNullOrEmpty(d.Type) ? null : d.Type,
                Size = (int)DiscoveryUnits.BytesToWholeGb(d.SizeBytes)
            })
            .ToList();
    }

    private static SystemResource ToResource(
        ProxmoxGuest guest,
        string scope,
        IReadOnlyDictionary<string, string> hypervisorNames,
        IReadOnlyDictionary<string, IReadOnlyList<ProxmoxGpu>> gpusByNode,
        ISet<string> taken) {
        // The vmid is unique within the cluster and survives a rename or a migration
        // between nodes, which is exactly what an identity needs to do.
        var discoveryId = DiscoveryId.Create(Scheme, $"{scope}/{guest.VmId}");

        return new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = DiscoveryNaming.Unique(
                DiscoveryNaming.Suggest(FallbackName(guest), "system", discoveryId),
                discoveryId,
                taken),
            DiscoveryId = discoveryId,
            Type = guest.Type,
            Os = guest.Os,
            Cores = guest.Cores > 0 ? guest.Cores : null,
            Ram = ToGb(guest.MemoryBytes),
            Ip = guest.Ip,
            Drives = ToGuestDrives(guest),
            Tags = guest.Tags.ToArray(),
            Labels = PassthroughLabels(guest, gpusByNode),
            RunsOn = hypervisorNames.TryGetValue(guest.Node, out var hypervisor) ? [hypervisor] : []
        };
    }

    /// <summary>
    ///     Every disk the config lists, falling back to the boot disk from the guest list
    ///     when the config could not be read. The storage backend says nothing about the
    ///     underlying medium, so the type is left off rather than guessed.
    /// </summary>
    private static List<Drive>? ToGuestDrives(ProxmoxGuest guest) {
        IReadOnlyList<long> sizes = guest.Disks.Count > 0
            ? guest.Disks
            : guest.DiskBytes > 0
                ? [guest.DiskBytes]
                : [];

        if (sizes.Count == 0)
            return null;

        return sizes
            .Select(bytes => new Drive { Size = (int)DiscoveryUnits.BytesToWholeGb(bytes) })
            .ToList();
    }

    /// <summary>
    ///     Records the cards a guest holds. The GPU itself stays on the Server, because
    ///     that is where it is physically installed; this is the assignment, which
    ///     RackPeek has no first-class way to express.
    /// </summary>
    private static Dictionary<string, string> PassthroughLabels(
        ProxmoxGuest guest,
        IReadOnlyDictionary<string, IReadOnlyList<ProxmoxGpu>> gpusByNode) {
        var labels = new Dictionary<string, string>();

        if (guest.PassthroughAddresses.Count == 0
            || !gpusByNode.TryGetValue(guest.Node, out IReadOnlyList<ProxmoxGpu>? gpus))
            return labels;

        var held = guest.PassthroughAddresses
            .Select(address => gpus.FirstOrDefault(g => AddressesMatch(g.Address, address)))
            .OfType<ProxmoxGpu>()
            .Select(g => g.Model)
            .ToList();

        if (held.Count == 0)
            return labels;

        var value = string.Join(", ", held);

        labels[GpuLabel] = value.Length <= _maxLabelLength
            ? value
            : value[.._maxLabelLength].TrimEnd(',', ' ');

        return labels;
    }

    /// <summary>
    ///     The device list gives a function suffix (<c>0000:01:00.0</c>) that a guest
    ///     config usually leaves off (<c>0000:01:00</c>), so either may be the longer.
    /// </summary>
    private static bool AddressesMatch(string deviceAddress, string configured) =>
        deviceAddress.StartsWith(configured, StringComparison.OrdinalIgnoreCase)
        || configured.StartsWith(deviceAddress, StringComparison.OrdinalIgnoreCase);

    /// <summary>A guest that was never named still needs one; the vmid is what people call it.</summary>
    private static string FallbackName(ProxmoxGuest guest) =>
        string.IsNullOrWhiteSpace(guest.Name)
            ? $"{(guest.Type == ProxmoxResponseParser.ContainerType ? "ct" : "vm")}-{guest.VmId}"
            : guest.Name;

    /// <summary><c>pve-manager/8.2.2/9355359c</c> becomes <c>Proxmox VE 8.2.2</c>.</summary>
    internal static string? DescribeVersion(string? pveVersion) {
        if (string.IsNullOrWhiteSpace(pveVersion))
            return null;

        var parts = pveVersion.Split('/');

        return parts.Length >= 2 ? $"Proxmox VE {parts[1]}" : pveVersion;
    }

    private static double? ToGb(long bytes) =>
        bytes > 0 ? DiscoveryUnits.BytesToWholeGb(bytes) : null;
}
