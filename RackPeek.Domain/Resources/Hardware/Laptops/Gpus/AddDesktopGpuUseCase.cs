using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Gpus;

public class AddLaptopGpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model,
        int? vram)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new InvalidOperationException($"Laptop '{name}' not found.");

        laptop.Gpus ??= new List<Gpu>();
        laptop.Gpus.Add(new Gpu
        {
            Model = model,
            Vram = vram
        });
        await repository.UpdateAsync(laptop);
    }
}