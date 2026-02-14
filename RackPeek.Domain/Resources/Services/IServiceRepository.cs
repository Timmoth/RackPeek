namespace RackPeek.Domain.Resources.Services;

public interface IServiceRepository : IResourceRepo<Service> 
{
    Task<int> GetCountAsync();
    Task<int> GetIpAddressCountAsync();

    Task<IReadOnlyList<Service>> GetBySystemHostAsync(string name);
}


public interface IResourceRepo<T> where T : Resource
{
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T service);
    Task UpdateAsync(T service);
    Task DeleteAsync(string name);
    Task<T?> GetByNameAsync(string name);
}