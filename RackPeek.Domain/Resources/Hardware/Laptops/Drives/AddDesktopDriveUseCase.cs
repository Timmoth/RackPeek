using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Drives;

public class AddLaptopDriveUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? type,
        int? size)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new NotFoundException($"Laptop '{name}' not found.");

        laptop.Drives ??= new List<Drive>();
        laptop.Drives.Add(new Drive
        {
            Type = type,
            Size = size
        });
        await repository.UpdateAsync(laptop);
    }
}