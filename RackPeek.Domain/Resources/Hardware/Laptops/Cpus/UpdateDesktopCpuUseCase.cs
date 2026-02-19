using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops.Cpus;

public class UpdateLaptopCpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int index,
        string? model,
        int? cores,
        int? threads)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var laptop = await repository.GetByNameAsync(name) as Laptop
                     ?? throw new NotFoundException($"Laptop '{name}' not found.");

        if (laptop.Cpus == null || index < 0 || index >= laptop.Cpus.Count)
            throw new NotFoundException($"CPU index {index} not found on Laptop '{name}'.");

        var cpu = laptop.Cpus[index];
        cpu.Model = model;
        cpu.Cores = cores;
        cpu.Threads = threads;
        await repository.UpdateAsync(laptop);
    }
}