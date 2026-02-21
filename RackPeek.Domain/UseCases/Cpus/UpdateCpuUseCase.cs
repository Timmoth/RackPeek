using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace RackPeek.Domain.UseCases.Cpus;

public interface IUpdateCpuUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        int index,
        string? model,
        int? cores,
        int? threads);
}

public class UpdateCpuUseCase<T>(IResourceCollection repo) : IUpdateCpuUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(
        string name,
        int index,
        string? model,
        int? cores,
        int? threads)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var resource = await repo.GetByNameAsync<T>(name) ??
                       throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not ICpuResource cpuResource) return;

        
        cpuResource.Cpus ??= [];

        if (index < 0)
            throw new NotFoundException($"Please pick a CPU index >= 0 for '{name}'.");
        
        if (cpuResource.Cpus.Count == 0)
            throw new NotFoundException($"'{name}' has no CPUs.");
        
        if (index >= cpuResource.Cpus.Count)
            throw new NotFoundException($"Please pick a CPU index < {cpuResource.Cpus.Count} for '{name}'.");

        var cpu = cpuResource.Cpus[index];
        cpu.Model = model;
        cpu.Cores = cores;
        cpu.Threads = threads;

        await repo.UpdateAsync(resource);
    }
}