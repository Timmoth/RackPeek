using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Cpus;

public class UpdateDesktopCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string desktopName, int index, Cpu updated)
    {
        var desktop = await repository.GetByNameAsync(desktopName) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{desktopName}' not found.");

        if (desktop.Cpus == null || index < 0 || index >= desktop.Cpus.Count)
            throw new InvalidOperationException($"CPU index {index} not found on desktop '{desktopName}'.");

        desktop.Cpus[index] = updated;

        await repository.UpdateAsync(desktop);
    }
}