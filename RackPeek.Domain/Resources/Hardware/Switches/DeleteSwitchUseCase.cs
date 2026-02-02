using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public class DeleteSwitchUseCase(IHardwareRepository repository, ISystemRepository systemsRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        if (await repository.GetByNameAsync(name) is not Switch hardware)
            throw new NotFoundException($"Switch '{name}' not found.");

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