using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Cpus;

public class RemoveLaptopCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new InvalidOperationException($"Laptop '{name}' not found.");

        if (laptop.Cpus == null || index < 0 || index >= laptop.Cpus.Count)
            throw new InvalidOperationException($"CPU index {index} not found on Laptop '{name}'.");

        laptop.Cpus.RemoveAt(index);

        await repository.UpdateAsync(laptop);
    }
}