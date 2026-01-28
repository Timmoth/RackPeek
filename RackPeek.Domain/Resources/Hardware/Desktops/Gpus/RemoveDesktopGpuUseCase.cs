using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Gpus;

public class RemoveDesktopGpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string desktopName, int index)
    {
        var desktop = await repository.GetByNameAsync(desktopName) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{desktopName}' not found.");

        if (desktop.Gpus == null || index < 0 || index >= desktop.Gpus.Count)
            throw new InvalidOperationException($"GPU index {index} not found on desktop '{desktopName}'.");

        desktop.Gpus.RemoveAt(index);

        await repository.UpdateAsync(desktop);
    }
}