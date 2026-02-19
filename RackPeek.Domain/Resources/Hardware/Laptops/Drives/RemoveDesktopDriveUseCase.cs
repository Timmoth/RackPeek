using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Drives;

public class RemoveLaptopDriveUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new NotFoundException($"Laptop '{name}' not found.");

        if (laptop.Drives == null || index < 0 || index >= laptop.Drives.Count)
            throw new NotFoundException($"Drive index {index} not found on Laptop '{name}'.");

        laptop.Drives.RemoveAt(index);

        await repository.UpdateAsync(laptop);
    }
}