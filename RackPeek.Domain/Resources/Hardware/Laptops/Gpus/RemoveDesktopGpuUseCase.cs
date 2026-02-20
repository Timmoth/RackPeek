using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Gpus;

public class RemoveLaptopGpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new NotFoundException($"Laptop '{name}' not found.");

        if (laptop.Gpus == null || index < 0 || index >= laptop.Gpus.Count)
            throw new NotFoundException($"GPU index {index} not found on Laptop '{name}'.");

        laptop.Gpus.RemoveAt(index);

        await repository.UpdateAsync(laptop);
    }
}