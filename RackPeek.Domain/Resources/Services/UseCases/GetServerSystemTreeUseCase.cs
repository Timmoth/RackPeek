using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class GetSystemServiceTreeUseCase(
    ISystemRepository systemRepository,
    IServiceRepository serviceRepository)
{
    public async Task<SystemDependencyTree?> ExecuteAsync(string systemName)
    {
        var system = await systemRepository.GetByNameAsync(systemName);
        if (system is null) return null;

        var services = await serviceRepository.GetBySystemHostAsync(system.Name);

        return new SystemDependencyTree(system, services);
        
    }
}
