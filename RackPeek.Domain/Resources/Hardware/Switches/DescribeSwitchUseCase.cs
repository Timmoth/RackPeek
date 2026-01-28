using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public record SwitchDescription(
    string Name,
    string? Model,
    bool? Managed,
    bool? Poe,
    int TotalPorts,
    int TotalSpeedGb,
    string PortSummary
);

public class DescribeSwitchUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<SwitchDescription?> ExecuteAsync(string name)
    {
        var switchResource = await repository.GetByNameAsync(name) as Switch;
        if (switchResource == null)
            return null;

        // If no ports exist, return defaults
        var ports = switchResource.Ports ?? new List<Port>();

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

        return new SwitchDescription(
            switchResource.Name,
            switchResource.Model,
            switchResource.Managed,
            switchResource.Poe,
            totalPorts,
            totalSpeedGb,
            portSummary
        );
    }
}