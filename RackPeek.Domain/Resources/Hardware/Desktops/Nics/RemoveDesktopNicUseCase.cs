using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public class RemoveDesktopNicUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string desktopName, int index)
    {
        var desktop = await repository.GetByNameAsync(desktopName) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{desktopName}' not found.");

        if (desktop.Nics == null || index < 0 || index >= desktop.Nics.Count)
            throw new InvalidOperationException($"NIC index {index} not found on desktop '{desktopName}'.");

        desktop.Nics.RemoveAt(index);

        await repository.UpdateAsync(desktop);
    }
}