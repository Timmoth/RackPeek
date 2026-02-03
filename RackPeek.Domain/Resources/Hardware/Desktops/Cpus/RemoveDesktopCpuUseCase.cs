using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Cpus;

public class RemoveDesktopCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        if (desktop.Cpus == null || index < 0 || index >= desktop.Cpus.Count)
            throw new NotFoundException($"CPU index {index} not found on desktop '{name}'.");

        desktop.Cpus.RemoveAt(index);

        await repository.UpdateAsync(desktop);
    }
}