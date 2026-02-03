using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Gpus;

public class AddDesktopGpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, Gpu gpu)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        desktop.Gpus ??= new List<Gpu>();
        desktop.Gpus.Add(gpu);

        await repository.UpdateAsync(desktop);
    }
}