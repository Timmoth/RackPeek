using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops;

public class UpdateLaptopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        string? notes = null
    )
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop;
        if (laptop == null)
            throw new NotFoundException($"Laptop '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            laptop.Model = model;

        if (notes != null)
        {
            laptop.Notes = notes;
        }
        await repository.UpdateAsync(laptop);
    }
}