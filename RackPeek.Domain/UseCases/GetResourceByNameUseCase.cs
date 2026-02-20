using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases;

public interface IGetResourceByNameUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task<T> ExecuteAsync(string name);
}

public class GetResourceByNameUseCase<T>(IResourceCollection repo) : IGetResourceByNameUseCase<T> where T : Resource
{
    public async Task<T> ExecuteAsync(string name)
    {
        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);

        if (await repo.GetByNameAsync(name) is not T resource)
            throw new NotFoundException($"Resource '{name}' not found.");

        return resource;
    }
}