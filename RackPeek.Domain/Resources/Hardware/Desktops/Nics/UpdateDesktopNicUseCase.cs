using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public class UpdateDesktopNicUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string desktopName, int index, Nic updated)
    {
        var desktop = await repository.GetByNameAsync(desktopName) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{desktopName}' not found.");

        if (desktop.Nics == null || index < 0 || index >= desktop.Nics.Count)
            throw new InvalidOperationException($"NIC index {index} not found on desktop '{desktopName}'.");

        desktop.Nics[index] = updated;

        await repository.UpdateAsync(desktop);
    }
}