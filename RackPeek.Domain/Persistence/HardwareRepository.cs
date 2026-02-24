using RackPeek.Domain.Resources.Hardware;

namespace RackPeek.Domain.Persistence;

public class YamlHardwareRepository(IResourceCollection resources) : IHardwareRepository
{
    public Task<int> GetCountAsync()
    {
        return Task.FromResult(resources.HardwareResources.Count);
    }

    public Task<Dictionary<string, int>> GetKindCountAsync()
    {
        return Task.FromResult(resources.HardwareResources
            .GroupBy(h => h.Kind)
            .ToDictionary(k => k.Key, v => v.Count()));
    }

    public Task<List<HardwareTree>> GetTreeAsync()
    {
        var hardwareTree = new List<HardwareTree>();

        // List<string> systemGroups = resources.SystemResources.ToList<string>();
            // .Where(s => s.RunsOn != null)
            // .ToList();

        var systemGroups = resources.SystemResources
            // .Where(s => !string.IsNullOrWhiteSpace(s.RunsOn))
            // .Where(s => s.RunsOn != null)
            // TODO: Get rid of 'First'
            .GroupBy(s => s.RunsOn.First().Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var serviceGroups = resources.ServiceResources
            // .Where(s => !string.IsNullOrWhiteSpace(s.RunsOn))
            // TODO: Get rid of 'First'
            .GroupBy(s => s.RunsOn.First().Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var hardware in resources.HardwareResources)
        {
            var systems = new List<SystemTree>();
            var hardwareKey = hardware.Name.Trim();

            if (systemGroups.TryGetValue(hardwareKey, out var systemResources))
                foreach (var system in systemResources)
                {
                    var services = new List<string>();
                    var systemKey = system.Name.Trim();

                    if (serviceGroups.TryGetValue(systemKey, out var serviceResources))
                        services.AddRange(serviceResources.Select(s => s.Name));

                    systems.Add(new SystemTree
                    {
                        SystemName = system.Name,
                        Services = services
                    });
                }

            hardwareTree.Add(new HardwareTree
            {
                Kind = hardware.Kind,
                HardwareName = hardware.Name,
                Systems = systems
            });
        }

        return Task.FromResult(hardwareTree);
    }
}
