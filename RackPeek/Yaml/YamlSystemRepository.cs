using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Yaml;

public class YamlSystemRepository(YamlResourceCollection resources) : ISystemRepository
{
    public Task<IReadOnlyList<SystemResource>> GetAllAsync()
    {
        return Task.FromResult(resources.SystemResources);
    }

    public Task<SystemResource?> GetByNameAsync(string name)
    {
        return Task.FromResult(resources.GetByName(name) as SystemResource);
    }

    public Task AddAsync(SystemResource systemResource)
    {
        if (resources.SystemResources.Any(r =>
                r.Name.Equals(systemResource.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"System with name '{systemResource.Name}' already exists.");

        // Use first file as default for new resources
        var targetFile = resources.SourceFiles.FirstOrDefault()
                         ?? throw new InvalidOperationException("No YAML file loaded.");

        resources.Add(systemResource, targetFile);
        resources.SaveAll();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(SystemResource systemResource)
    {
        var existing = resources.SystemResources
            .FirstOrDefault(r => r.Name.Equals(systemResource.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"System '{systemResource.Name}' not found.");

        resources.Update(systemResource);
        resources.SaveAll();

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name)
    {
        var existing = resources.SystemResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"System '{name}' not found.");

        resources.Delete(name);
        resources.SaveAll();

        return Task.CompletedTask;
    }
}