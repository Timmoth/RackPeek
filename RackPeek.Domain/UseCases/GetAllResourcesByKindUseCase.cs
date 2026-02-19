using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases;

public interface IGetAllResourcesByKindUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task<IReadOnlyList<T>> ExecuteAsync();
}


public class GetAllResourcesByKindUseCase<T>(IResourceCollection repo) : IGetAllResourcesByKindUseCase<T> where T : Resource
{
    public async Task<IReadOnlyList<T>> ExecuteAsync()
    {
        return await repo.GetAllOfTypeAsync<T>();;
    }
}