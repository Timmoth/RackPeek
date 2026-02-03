using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class GetSystemServiceTreeUseCase(
    ISystemRepository systemRepository,
    IServiceRepository serviceRepository) : IUseCase
{
    public async Task<SystemDependencyTree> ExecuteAsync(string name)
    {
        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);
        var system = await systemRepository.GetByNameAsync(name);
        if (system is null)
        {
            throw new NotFoundException($"System '{name}' not found.");
        }
        var services = await serviceRepository.GetBySystemHostAsync(system.Name);

        return new SystemDependencyTree(system, services);
    }
}