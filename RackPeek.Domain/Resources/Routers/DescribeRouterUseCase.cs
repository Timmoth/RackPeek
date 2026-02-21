using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Routers;

public record RouterDescription(
    string Name,
    string? Model,
    bool? Managed,
    bool? Poe,
    int TotalPorts,
    double TotalSpeedGb,
    string PortSummary
);

public class DescribeRouterUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<RouterDescription> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var routerResource = await repository.GetByNameAsync(name) as Router;
        if (routerResource == null)
            throw new NotFoundException($"Router '{name}' not found.");

        // If no ports exist, return defaults
        var ports = routerResource.Ports ?? new List<Port>();

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

        return new RouterDescription(
            routerResource.Name,
            routerResource.Model,
            routerResource.Managed,
            routerResource.Poe,
            totalPorts,
            totalSpeedGb,
            portSummary
        );
    }
}