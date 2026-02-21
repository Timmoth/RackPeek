namespace RackPeek.Domain.Resources.Services;

public interface IServiceRepository
{
    Task<int> GetCountAsync();
    Task<int> GetIpAddressCountAsync();

    Task<IReadOnlyList<Service>> GetBySystemHostAsync(string name);
}