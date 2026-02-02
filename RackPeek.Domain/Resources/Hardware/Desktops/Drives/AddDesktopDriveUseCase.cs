using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Drives;

public class AddDesktopDriveUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, Drive drive)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{name}' not found.");

        desktop.Drives ??= new List<Drive>();
        desktop.Drives.Add(drive);

        await repository.UpdateAsync(desktop);
    }
}