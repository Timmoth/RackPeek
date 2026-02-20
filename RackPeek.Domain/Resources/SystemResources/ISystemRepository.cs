namespace RackPeek.Domain.Resources.SystemResources;

public interface ISystemRepository
{
    Task<int> GetSystemCountAsync();
    Task<Dictionary<string, int>> GetSystemTypeCountAsync();
    Task<Dictionary<string, int>> GetSystemOsCountAsync();
    Task<IReadOnlyList<SystemResource>> GetFilteredAsync(string? typeFilter, string? osFilter);
    Task<IReadOnlyList<SystemResource>> GetByPhysicalHostAsync(string name);
}