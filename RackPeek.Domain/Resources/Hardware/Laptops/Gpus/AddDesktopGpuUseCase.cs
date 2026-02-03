using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Gpus;

public class AddLaptopGpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, Gpu gpu)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new InvalidOperationException($"Laptop '{name}' not found.");

        laptop.Gpus ??= new List<Gpu>();
        laptop.Gpus.Add(gpu);

        await repository.UpdateAsync(laptop);
    }
}