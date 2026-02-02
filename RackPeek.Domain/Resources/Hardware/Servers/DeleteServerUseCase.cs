using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class DeleteServerUseCase(IHardwareRepository repository, ISystemRepository systemsRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name) as Server;
        if (hardware == null)
            throw new NotFoundException($"Server '{name}' not found.");

        // Break link to dependants
        var dependants = await systemsRepo.GetByPhysicalHostAsync(name);
        foreach (var systemResource in dependants)
        {
            systemResource.RunsOn = null;
            await systemsRepo.UpdateAsync(systemResource);
        }
        
        await repository.DeleteAsync(name);
    }
}