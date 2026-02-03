using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Cpus;

public class AddLaptopCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, string? model, int? cores, int? threads)
    {
        // ToDo validate / normalize all inputs
        
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        
        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new NotFoundException($"Laptop '{name}' not found.");

        laptop.Cpus ??= new List<Cpu>();
        laptop.Cpus.Add(new Cpu { Model = model, Cores = cores, Threads = threads });

        await repository.UpdateAsync(laptop);
    }
}