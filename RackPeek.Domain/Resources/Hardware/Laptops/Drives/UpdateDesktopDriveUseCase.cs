using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Drives;

public class UpdateLaptopDriveUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index, string? type, int? size)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new NotFoundException($"Laptop '{name}' not found.");

        if (laptop.Drives == null || index < 0 || index >= laptop.Drives.Count)
            throw new NotFoundException($"Drive index {index} not found on Laptop '{name}'.");

        var drive = laptop.Drives[index];
        drive.Type = type;
        drive.Size = size;

        await repository.UpdateAsync(laptop);
    }
}