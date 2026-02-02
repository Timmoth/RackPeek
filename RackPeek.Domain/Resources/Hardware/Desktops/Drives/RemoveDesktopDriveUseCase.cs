using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Drives;

public class RemoveDesktopDriveUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{name}' not found.");

        if (desktop.Drives == null || index < 0 || index >= desktop.Drives.Count)
            throw new InvalidOperationException($"Drive index {index} not found on desktop '{name}'.");

        desktop.Drives.RemoveAt(index);

        await repository.UpdateAsync(desktop);
    }
}