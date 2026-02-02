using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Drives;

public class AddLaptopDriveUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, Drive drive)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new InvalidOperationException($"Laptop '{name}' not found.");

        laptop.Drives ??= new List<Drive>();
        laptop.Drives.Add(drive);

        await repository.UpdateAsync(laptop);
    }
}