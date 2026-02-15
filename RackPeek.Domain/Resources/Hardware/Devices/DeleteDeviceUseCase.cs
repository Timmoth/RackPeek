using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware.Devices;

public class DeleteDeviceUseCase(IHardwareRepository repository, ISystemRepository systemsRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Device)
            throw new NotFoundException($"Device '{name}' not found.");

        var dependants = await systemsRepo.GetByPhysicalHostAsync(name);
        foreach (var systemResource in dependants)
        {
            systemResource.RunsOn = null;
            await systemsRepo.UpdateAsync(systemResource);
        }

        await repository.DeleteAsync(name);
    }
}
