namespace RackPeek.Domain.Resources.SystemResources;

public interface ISystemRepository
{
    Task<int> GetSystemCountAsync();
    Task<Dictionary<string, int>> GetSystemTypeCountAsync();
    Task<Dictionary<string, int>> GetSystemOsCountAsync();

    Task<IReadOnlyList<SystemResource>> GetAllAsync();
    Task<IReadOnlyList<SystemResource>> GetFilteredAsync(string? typeFilter, string? osFilter);

    Task AddAsync(SystemResource systemResource);
    Task UpdateAsync(SystemResource systemResource);
    Task DeleteAsync(string name);
    Task<SystemResource?> GetByNameAsync(string name);
    Task<IReadOnlyList<SystemResource>> GetByPhysicalHostAsync(string name);
}