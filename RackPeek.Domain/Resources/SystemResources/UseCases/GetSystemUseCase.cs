using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class GetSystemUseCase(ISystemRepository repository) : IUseCase
{
    public async Task<SystemResource> ExecuteAsync(string name)
    {
        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);
        var system = await repository.GetByNameAsync(name);

        if (system == null)
        {
            throw new NotFoundException($"System '{name}' not found.");
        }
        
        return system;
    }
}