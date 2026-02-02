using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Firewalls;

public record FirewallDescription(
    string Name,
    string? Model,
    bool? Managed,
    bool? Poe,
    int TotalPorts,
    int TotalSpeedGb,
    string PortSummary
);

public class DescribeFirewallUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<FirewallDescription?> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var firewallResource = await repository.GetByNameAsync(name) as Firewall;
        if (firewallResource == null)
            return null;

        // If no ports exist, return defaults
        var ports = firewallResource.Ports ?? new List<Port>();

        // Total ports count
        var totalPorts = ports.Sum(p => p.Count ?? 0);

        // Total speed (sum of each port speed * count)
        var totalSpeedGb = ports.Sum(p => (p.Speed ?? 0) * (p.Count ?? 0));

        // Build a port summary string
        var portGroups = ports
            .GroupBy(p => p.Type ?? "Unknown")
            .Select(g =>
            {
                var count = g.Sum(x => x.Count ?? 0);
                var speed = g.Sum(x => (x.Speed ?? 0) * (x.Count ?? 0));
                return $"{g.Key}: {count} ports ({speed} Gb total)";
            });

        var portSummary = string.Join(", ", portGroups);

        return new FirewallDescription(
            firewallResource.Name,
            firewallResource.Model,
            firewallResource.Managed,
            firewallResource.Poe,
            totalPorts,
            totalSpeedGb,
            portSummary
        );
    }
}