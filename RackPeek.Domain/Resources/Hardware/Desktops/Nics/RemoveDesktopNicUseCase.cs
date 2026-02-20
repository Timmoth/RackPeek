using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public class RemoveDesktopNicUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        if (desktop.Nics == null || index < 0 || index >= desktop.Nics.Count)
            throw new NotFoundException($"NIC index {index} not found on desktop '{name}'.");

        desktop.Nics.RemoveAt(index);

        await repository.UpdateAsync(desktop);
    }
}