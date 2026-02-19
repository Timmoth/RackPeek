using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Gpus;

public class UpdateLaptopGpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int index,
        string? model,
        int? vram)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new NotFoundException($"Laptop '{name}' not found.");

        if (laptop.Gpus == null || index < 0 || index >= laptop.Gpus.Count)
            throw new NotFoundException($"GPU index {index} not found on Laptop '{name}'.");

        var gpu = laptop.Gpus[index];
        gpu.Model = model;
        gpu.Vram = vram;
        await repository.UpdateAsync(laptop);
    }
}