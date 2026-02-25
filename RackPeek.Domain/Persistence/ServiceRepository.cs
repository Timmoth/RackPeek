using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Persistence;

public class ServiceRepository(IResourceCollection resources) : IServiceRepository
{
    public Task<int> GetCountAsync()
    {
        return Task.FromResult(resources.ServiceResources.Count);
    }

    public Task<int> GetIpAddressCountAsync()
    {
        return Task.FromResult(resources.ServiceResources
            .Where(i => i.Network?.Ip != null)
            .Select(i => i.Network!.Ip)
            .Distinct()
            .Count());
    }

    public Task<IReadOnlyList<Service>> GetBySystemHostAsync(string systemHostName)
    {
        var systemHostNameLower = systemHostName.ToLower().Trim();
        var results = resources.ServiceResources
            .Where(s => s.RunsOn.Select(p => p.ToLower().Equals(systemHostNameLower)).ToList().Count > 0).ToList();
        return Task.FromResult<IReadOnlyList<Service>>(results);
    }

    public Task<IReadOnlyList<Service>> GetAllAsync()
    {
        return Task.FromResult(resources.ServiceResources);
    }

    public Task<Service?> GetByNameAsync(string name)
    {
        return Task.FromResult(resources.GetByName(name) as Service);
    }

    public async Task AddAsync(Service service)
    {
        if (resources.ServiceResources.Any(r =>
                r.Name.Equals(service.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"Service with name '{service.Name}' already exists.");

        await resources.AddAsync(service);
    }

    public async Task UpdateAsync(Service service)
    {
        var existing = resources.ServiceResources
            .FirstOrDefault(r => r.Name.Equals(service.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Service '{service.Name}' not found.");

        await resources.UpdateAsync(service);
    }

    public async Task DeleteAsync(string name)
    {
        var existing = resources.ServiceResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Service '{name}' not found.");

        await resources.DeleteAsync(name);
    }
}
