using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Persistence;

public class YamlSystemRepository(IResourceCollection resources) : ISystemRepository {
    public Task<int> GetSystemCountAsync() => Task.FromResult(resources.SystemResources.Count);

    public Task<Dictionary<string, int>> GetSystemTypeCountAsync() {
        return Task.FromResult(resources.SystemResources
            .Where(s => !string.IsNullOrEmpty(s.Type))
            .GroupBy(h => h.Type!)
            .ToDictionary(k => k.Key, v => v.Count()));
    }

    public Task<Dictionary<string, int>> GetSystemOsCountAsync() {
        return Task.FromResult(resources.SystemResources
            .Where(s => !string.IsNullOrEmpty(s.Os))
            .GroupBy(h => h.Os!)
            .ToDictionary(k => k.Key, v => v.Count()));
    }

    public Task<IReadOnlyList<SystemResource>> GetFilteredAsync(
        string? typeFilter,
        string? osFilter) {
        IQueryable<SystemResource> query = resources.SystemResources.AsQueryable();

        var type = Normalize(typeFilter);
        var os = Normalize(osFilter);

        if (type != null)
            query = query.Where(x => x.Type != null && x.Type.Equals(type, StringComparison.CurrentCultureIgnoreCase));

        if (os != null)
            query = query.Where(x => x.Os != null && x.Os.Equals(os, StringComparison.CurrentCultureIgnoreCase));

        var results = query.ToList();
        return Task.FromResult<IReadOnlyList<SystemResource>>(results);
    }

    public Task<IReadOnlyList<SystemResource>> GetByPhysicalHostAsync(string physicalHostName) {
        var physicalHostNameLower = physicalHostName.ToLower().Trim();
        var results = resources.SystemResources
            .Where(s => s.RunsOn.Select(sys => sys.ToLower().Equals(physicalHostNameLower)).ToList().Count > 0)
            .ToList();
        return Task.FromResult<IReadOnlyList<SystemResource>>(results);
    }

    public Task<IReadOnlyList<SystemResource>> GetAllAsync() => Task.FromResult(resources.SystemResources);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLower();


    public Task<SystemResource?> GetByNameAsync(string name) =>
        Task.FromResult(resources.GetByName(name) as SystemResource);

    public async Task AddAsync(SystemResource systemResource) {
        if (resources.SystemResources.Any(r =>
                r.Name.Equals(systemResource.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"System with name '{systemResource.Name}' already exists.");

        await resources.AddAsync(systemResource);
    }

    public async Task UpdateAsync(SystemResource systemResource) {
        SystemResource? existing = resources.SystemResources
            .FirstOrDefault(r => r.Name.Equals(systemResource.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"System '{systemResource.Name}' not found.");

        await resources.UpdateAsync(systemResource);
    }

    public async Task DeleteAsync(string name) {
        SystemResource? existing = resources.SystemResources
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"System '{name}' not found.");

        await resources.DeleteAsync(name);
    }
}
