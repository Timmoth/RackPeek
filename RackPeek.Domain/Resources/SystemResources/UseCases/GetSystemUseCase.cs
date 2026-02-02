using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class GetSystemUseCase(ISystemRepository repository) : IUseCase
{
    public async Task<SystemResource?> ExecuteAsync(string name)
    {
        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);
        return await repository.GetByNameAsync(name);
    }
}