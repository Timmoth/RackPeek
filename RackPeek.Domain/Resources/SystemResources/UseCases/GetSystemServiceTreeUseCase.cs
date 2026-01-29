using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class GetSystemServiceTreeUseCase(
    ISystemRepository systemRepository,
    IServiceRepository serviceRepository) : IUseCase
{
    public async Task<SystemDependencyTree?> ExecuteAsync(string systemName)
    {
        var system = await systemRepository.GetByNameAsync(systemName);
        if (system is null) return null;

        var services = await serviceRepository.GetBySystemHostAsync(system.Name);

        return new SystemDependencyTree(system, services);
    }
}