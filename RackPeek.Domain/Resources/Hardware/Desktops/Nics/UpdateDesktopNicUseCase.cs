using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public class UpdateDesktopNicUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index, Nic updated)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        if (desktop.Nics == null || index < 0 || index >= desktop.Nics.Count)
            throw new NotFoundException($"NIC index {index} not found on desktop '{name}'.");

        desktop.Nics[index] = updated;

        await repository.UpdateAsync(desktop);
    }
}