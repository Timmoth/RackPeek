using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops;

public class AddLaptopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new InvalidOperationException($"Laptop '{name}' already exists.");

        var laptop = new Laptop
        {
            Name = name,
            Cpus = new List<Cpu>(),
            Drives = new List<Drive>(),
            Gpus = new List<Gpu>(),
            Ram = null
        };

        await repository.AddAsync(laptop);
    }
}