using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Cpus;

public class RemoveDesktopCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string desktopName, int index)
    {
        var desktop = await repository.GetByNameAsync(desktopName) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{desktopName}' not found.");

        if (desktop.Cpus == null || index < 0 || index >= desktop.Cpus.Count)
            throw new InvalidOperationException($"CPU index {index} not found on desktop '{desktopName}'.");

        desktop.Cpus.RemoveAt(index);

        await repository.UpdateAsync(desktop);
    }
}