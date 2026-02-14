using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Persistence;

public class YamlHardwareRepo<T>(IResourceCollection resources) : IResourceRepo<T> where T : Hardware
{
    public Task<IReadOnlyList<T>> GetAllAsync()
    {
        var servers = resources.HardwareResources.OfType<T>().ToList();
        return Task.FromResult<IReadOnlyList<T>>(servers.AsReadOnly());
    }

    public async Task AddAsync(T service)
    {
        if (await resources.Exists(service.Name))
            throw new InvalidOperationException(
                $"Resource with name '{service.Name}' already exists.");

        await resources.AddAsync(service);
    }
    
    public async Task UpdateAsync(T service)
    {
        var existing = resources.HardwareResources
            .FirstOrDefault(r => r.Name.Equals(service.Name, StringComparison.OrdinalIgnoreCase));
        
        if (existing is not T)
            throw new InvalidOperationException($"'{service.Name}' not found.");

        await resources.UpdateAsync(service);
        
    }

    public async Task DeleteAsync(string name)
    {
        var existing = resources.HardwareResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing is not Server)
            throw new InvalidOperationException($"'{name}' not found.");

        await resources.DeleteAsync(name);
        
    }

    public Task<T?> GetByNameAsync(string name)
    {
        return Task.FromResult(resources.GetByName(name) as T);
    }
}