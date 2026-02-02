using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Gpus;

public class UpdateLaptopGpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index, Gpu updated)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new InvalidOperationException($"Laptop '{name}' not found.");

        if (laptop.Gpus == null || index < 0 || index >= laptop.Gpus.Count)
            throw new InvalidOperationException($"GPU index {index} not found on Laptop '{name}'.");

        laptop.Gpus[index] = updated;

        await repository.UpdateAsync(laptop);
    }
}