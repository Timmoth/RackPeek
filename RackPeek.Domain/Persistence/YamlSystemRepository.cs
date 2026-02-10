using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Persistence;

public class YamlSystemRepository(IResourceCollection resources) : ISystemRepository
{
    public Task<int> GetSystemCountAsync()
    {
        return Task.FromResult(resources.SystemResources.Count);
    }

    public Task<Dictionary<string, int>> GetSystemTypeCountAsync()
    {
        return Task.FromResult(resources.SystemResources
            .Where(s => !string.IsNullOrEmpty(s.Type))
            .GroupBy(h => h.Type!)
            .ToDictionary(k => k.Key, v => v.Count()));
    }

    public Task<Dictionary<string, int>> GetSystemOsCountAsync()
    {
        return Task.FromResult(resources.SystemResources
            .Where(s => !string.IsNullOrEmpty(s.Os))
            .GroupBy(h => h.Os!)
            .ToDictionary(k => k.Key, v => v.Count()));
    }

    public Task<IReadOnlyList<SystemResource>> GetAllAsync()
    {
        return Task.FromResult(resources.SystemResources);
    }

    public Task<IReadOnlyList<SystemResource>> GetFilteredAsync(
        string? typeFilter,
        string? osFilter)
    {
        var query = resources.SystemResources.AsQueryable();

        var type = Normalize(typeFilter);
        var os = Normalize(osFilter);

        if (type != null)
            query = query.Where(x => x.Type != null && x.Type.Equals(type, StringComparison.CurrentCultureIgnoreCase));

        if (os != null)
            query = query.Where(x => x.Os != null && x.Os.Equals(os, StringComparison.CurrentCultureIgnoreCase));

        var results = query.ToList();
        return Task.FromResult<IReadOnlyList<SystemResource>>(results);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLower();
    }


    public Task<SystemResource?> GetByNameAsync(string name)
    {
        return Task.FromResult(resources.GetByName(name) as SystemResource);
    }

    public Task<IReadOnlyList<SystemResource>> GetByPhysicalHostAsync(string physicalHostName)
    {
        var physicalHostNameLower = physicalHostName.ToLower().Trim();
        var results = resources.SystemResources
            .Where(s => s.RunsOn != null && s.RunsOn.ToLower().Equals(physicalHostNameLower)).ToList();
        return Task.FromResult<IReadOnlyList<SystemResource>>(results);
    }

    public async Task AddAsync(SystemResource systemResource)
    {
        if (resources.SystemResources.Any(r =>
                r.Name.Equals(systemResource.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"System with name '{systemResource.Name}' already exists.");

        await resources.AddAsync(systemResource);
    }

    public async Task UpdateAsync(SystemResource systemResource)
    {
        var existing = resources.SystemResources
            .FirstOrDefault(r => r.Name.Equals(systemResource.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"System '{systemResource.Name}' not found.");

        await resources.UpdateAsync(systemResource);
    }

    public async Task DeleteAsync(string name)
    {
        var existing = resources.SystemResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"System '{name}' not found.");

        await resources.DeleteAsync(name);
    }
}