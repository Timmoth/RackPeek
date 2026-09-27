using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Discovery;

/// <summary>Maps host facts onto the System resource RackPeek stores. Pure.</summary>
public static class SystemResourceMapper {
    /// <summary>
    ///     <paramref name="nameOverride" /> is what makes repeated runs on a box stable
    ///     regardless of hostname changes, and is the recommended way to run this from a
    ///     timer. Without it the hostname is used.
    /// </summary>
    public static SystemResource ToResource(SystemFacts facts, string? nameOverride = null) {
        var discoveryId = DiscoveryId.Create(
            DiscoveryId.SystemScheme,
            facts.MachineId ?? facts.Hostname);

        var resource = new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = DiscoveryNaming.Suggest(
                nameOverride ?? DiscoveryNaming.HostLabel(facts.Hostname),
                "system",
                discoveryId),
            DiscoveryId = discoveryId,
            Type = facts.Type,
            Os = facts.Os,
            Cores = facts.Cores,
            Ram = facts.RamGb,
            Ip = facts.Ip,
            Drives = facts.Drives.Count == 0
                ? null
                : facts.Drives.Select(d => new Drive { Type = d.Type, Size = d.SizeGb }).ToList()
        };

        // The bridge to network discovery: a scan identifies this machine by one of
        // these, so carrying them lets the resolver land both collectors on one card.
        if (facts.Macs.Count > 0)
            resource.Labels["macs"] = string.Join(",", facts.Macs);

        return resource;
    }
}
