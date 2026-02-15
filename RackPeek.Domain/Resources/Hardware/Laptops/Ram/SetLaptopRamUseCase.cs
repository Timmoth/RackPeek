using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Ram;

public class SetLaptopRamUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int? ramGb = null,
        int? ramMts = null)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Laptop laptop)
            throw new NotFoundException($"Laptop '{name}' not found.");

        if (ramGb.HasValue)
        {
            ThrowIfInvalid.RamGb(ramGb);
            laptop.Ram ??= new Models.Ram();
            laptop.Ram.Size = ramGb.Value;
        }

        if (ramMts.HasValue)
        {
            laptop.Ram ??= new Models.Ram();
            laptop.Ram.Mts = ramMts.Value;
        }

        await repository.UpdateAsync(laptop);
    }
}
