using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Ram;

public class SetDesktopRamUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int? ramGb = null,
        int? ramMts = null)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Desktop desktop)
            throw new NotFoundException($"Desktop '{name}' not found.");

        if (ramGb.HasValue)
        {
            ThrowIfInvalid.RamGb(ramGb);
            desktop.Ram ??= new Models.Ram();
            desktop.Ram.Size = ramGb.Value;
        }

        if (ramMts.HasValue)
        {
            desktop.Ram ??= new Models.Ram();
            desktop.Ram.Mts = ramMts.Value;
        }

        await repository.UpdateAsync(desktop);
    }
}
