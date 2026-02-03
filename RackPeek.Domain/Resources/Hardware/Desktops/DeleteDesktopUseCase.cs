using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class DeleteDesktopUseCase(IHardwareRepository repository, ISystemRepository systemsRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware == null)
            throw new NotFoundException($"Desktop '{name}' not found.");

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