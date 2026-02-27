using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class GetSystemServiceTreeUseCase(
    IResourceCollection repo) : IUseCase
{
    public async Task<SystemDependencyTree> ExecuteAsync(string name)
    {
        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);
        var system = await repo.GetByNameAsync(name) as SystemResource;
        if (system is null) throw new NotFoundException($"System '{name}' not found.");
        var services = await repo.GetDependantsAsync(system.Name);

        return new SystemDependencyTree(system, services.OfType<Resource>());
    }
}