using RackPeek.Domain.Resources.Services;

namespace RackPeek.Yaml;

public class YamlServiceRepository(YamlResourceCollection resources) : IServiceRepository
{
    public Task<IReadOnlyList<Service>> GetAllAsync()
    {
        return Task.FromResult(resources.ServiceResources);
    }

    public Task<Service?> GetByNameAsync(string name)
    {
        return Task.FromResult(resources.GetByName(name) as Service);
    }

    public Task<IReadOnlyList<Service>> GetBySystemHostAsync(string systemHostName)
    {
        var systemHostNameLower = systemHostName.ToLower().Trim();
        var results = resources.ServiceResources
            .Where(s => s.RunsOn != null && s.RunsOn.ToLower().Equals(systemHostNameLower)).ToList();
        return Task.FromResult<IReadOnlyList<Service>>(results);
    }

    public Task AddAsync(Service service)
    {
        if (resources.ServiceResources.Any(r =>
                r.Name.Equals(service.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"Service with name '{service.Name}' already exists.");

        // Use first file as default for new resources
        var targetFile = resources.SourceFiles.FirstOrDefault()
                         ?? throw new InvalidOperationException("No YAML file loaded.");

        resources.Add(service, targetFile);
        resources.SaveAll();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Service service)
    {
        var existing = resources.ServiceResources
            .FirstOrDefault(r => r.Name.Equals(service.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Service '{service.Name}' not found.");

        resources.Update(service);
        resources.SaveAll();

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name)
    {
        var existing = resources.ServiceResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Service '{name}' not found.");

        resources.Delete(name);
        resources.SaveAll();

        return Task.CompletedTask;
    }
}