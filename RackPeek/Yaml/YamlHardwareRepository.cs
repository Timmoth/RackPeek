using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Yaml;

public class YamlHardwareRepository(YamlResourceCollection resources) : IHardwareRepository
{
    public Task<IReadOnlyList<Hardware>> GetAllAsync()
    {
        return Task.FromResult(resources.HardwareResources);
    }

    public Task<Hardware?> GetByNameAsync(string name)
    {
        return Task.FromResult(resources.GetByName(name) as Hardware);
    }

    public Task AddAsync(Hardware hardware)
    {
        if (resources.HardwareResources.Any(r =>
                r.Name.Equals(hardware.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"Hardware with name '{hardware.Name}' already exists.");

        // Use first file as default for new resources
        var targetFile = resources.SourceFiles.FirstOrDefault()
                         ?? throw new InvalidOperationException("No YAML file loaded.");

        resources.Add(hardware, targetFile);
        resources.SaveAll();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Hardware hardware)
    {
        var existing = resources.HardwareResources
            .FirstOrDefault(r => r.Name.Equals(hardware.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Hardware '{hardware.Name}' not found.");

        resources.Update(hardware);
        resources.SaveAll();

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name)
    {
        var existing = resources.HardwareResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Hardware '{name}' not found.");

        resources.Delete(name);
        resources.SaveAll();

        return Task.CompletedTask;
    }
}