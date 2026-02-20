using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Hardware.Laptops;

public class UpdateLaptopUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        int? ramGb = null,
        int? ramMts = null,
        string? notes = null
    )
    {
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop;
        if (laptop == null)
            throw new NotFoundException($"Laptop '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            laptop.Model = model;

        // ---- RAM ----
        if (ramGb.HasValue)
        {
            ThrowIfInvalid.RamGb(ramGb);
            laptop.Ram ??= new Ram();
            laptop.Ram.Size = ramGb.Value;
        }

        if (ramMts.HasValue)
        {
            laptop.Ram ??= new Ram();
            laptop.Ram.Mts = ramMts.Value;
        }
        if (notes != null)
        {
            laptop.Notes = notes;
        }
        await repository.UpdateAsync(laptop);
    }
}