using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Drives;

public class UpdateDesktopDriveUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string desktopName, int index, Drive updated)
    {
        var desktop = await repository.GetByNameAsync(desktopName) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{desktopName}' not found.");

        if (desktop.Drives == null || index < 0 || index >= desktop.Drives.Count)
            throw new InvalidOperationException($"Drive index {index} not found on desktop '{desktopName}'.");

        desktop.Drives[index] = updated;

        await repository.UpdateAsync(desktop);
    }
}