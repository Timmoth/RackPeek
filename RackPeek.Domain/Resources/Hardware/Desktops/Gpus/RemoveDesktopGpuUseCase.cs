using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Gpus;

public class RemoveDesktopGpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        if (desktop.Gpus == null || index < 0 || index >= desktop.Gpus.Count)
            throw new NotFoundException($"GPU index {index} not found on desktop '{name}'.");

        desktop.Gpus.RemoveAt(index);

        await repository.UpdateAsync(desktop);
    }
}