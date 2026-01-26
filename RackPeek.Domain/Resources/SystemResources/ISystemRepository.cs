namespace RackPeek.Domain.Resources.SystemResources;

public interface ISystemRepository
{
    Task<IReadOnlyList<SystemResource>> GetAllAsync();
    Task AddAsync(SystemResource systemResource);
    Task UpdateAsync(SystemResource systemResource);
    Task DeleteAsync(string name);
    Task<SystemResource?> GetByNameAsync(string name);
}

