using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Models;

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

    public Task<IReadOnlyList<Hardware>> GetAllAsync()
    {
        return Task.FromResult(resources.HardwareResources);
    }

    public Task<Hardware?> GetByNameAsync(string name)
    {
        return Task.FromResult(resources.GetByName(name) as Hardware);
    }

    public Task<List<HardwareTree>> GetTreeAsync()
    {
        var hardwareTree = new List<HardwareTree>();

        var systemGroups = resources.SystemResources
            .Where(s => !string.IsNullOrWhiteSpace(s.RunsOn))
            .GroupBy(s => s.RunsOn!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var serviceGroups = resources.ServiceResources
            .Where(s => !string.IsNullOrWhiteSpace(s.RunsOn))
            .GroupBy(s => s.RunsOn!.Trim(), StringComparer.OrdinalIgnoreCase)
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


    public async Task AddAsync(Hardware hardware)
    {
        if (resources.HardwareResources.Any(r =>
                r.Name.Equals(hardware.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"Hardware with name '{hardware.Name}' already exists.");

        await resources.AddAsync(hardware);
    }

    public async Task UpdateAsync(Hardware hardware)
    {
        var existing = resources.HardwareResources
            .FirstOrDefault(r => r.Name.Equals(hardware.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Hardware '{hardware.Name}' not found.");

        await resources.UpdateAsync(hardware);
    }

    public async Task DeleteAsync(string name)
    {
        var existing = resources.HardwareResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Hardware '{name}' not found.");

        await resources.DeleteAsync(name);
    }
}