using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Drives;

public class UpdateLaptopDriveUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index, Drive updated)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new InvalidOperationException($"Laptop '{name}' not found.");

        if (laptop.Drives == null || index < 0 || index >= laptop.Drives.Count)
            throw new InvalidOperationException($"Drive index {index} not found on Laptop '{name}'.");

        laptop.Drives[index] = updated;

        await repository.UpdateAsync(laptop);
    }
}