using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Yaml;

public class YamlHardwareRepository : IHardwareRepository
{
    private readonly YamlResourceCollection _resources;

    public YamlHardwareRepository(YamlResourceCollection resources)
    {
        _resources = resources;
    }

    public Task<IReadOnlyList<Hardware>> GetAllAsync()
        => Task.FromResult(_resources.HardwareResources);

    public Task<Hardware?> GetByNameAsync(string name)
        => Task.FromResult(_resources.GetByName(name) as Hardware);

    public Task AddAsync(Hardware hardware)
    {
        if (_resources.HardwareResources.Any(r =>
            r.Name.Equals(hardware.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Hardware with name '{hardware.Name}' already exists.");
        }

        // Use first file as default for new resources
        var targetFile = _resources.SourceFiles.FirstOrDefault()
                         ?? throw new InvalidOperationException("No YAML file loaded.");

        _resources.Add(hardware, targetFile);
        _resources.SaveAll();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Hardware hardware)
    {
        var existing = _resources.HardwareResources
            .FirstOrDefault(r => r.Name.Equals(hardware.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Hardware '{hardware.Name}' not found.");

        _resources.Update(hardware);
        _resources.SaveAll();

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name)
    {
        var existing = _resources.HardwareResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Hardware '{name}' not found.");

        _resources.Delete(name);
        _resources.SaveAll();

        return Task.CompletedTask;
    }
}
