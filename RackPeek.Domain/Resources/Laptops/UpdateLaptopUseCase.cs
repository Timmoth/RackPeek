using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Laptops;

public class UpdateLaptopUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        double? ramGb = null,
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
        
        if (laptop.Ram != null)
        {
            if (laptop.Ram.Size == 0)
            {
                laptop.Ram.Size = null;
            }
            
            if (laptop.Ram.Mts == 0)
            {
                laptop.Ram.Mts = null;
            }

            if (laptop.Ram.Size == null && laptop.Ram.Mts == null)
            {
                laptop.Ram = null;
            }
        }


        if (notes != null) laptop.Notes = notes;
        await repository.UpdateAsync(laptop);
    }
}