using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Models;

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

        var resource = await repo.GetByNameAsync(name);

        if (resource is not ICpuResource cpuResource) return;

        cpuResource.Cpus ??= [];

        if (index < 0 || index >= cpuResource.Cpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "CPU index out of range.");

        var cpu = cpuResource.Cpus[index];
        cpu.Model = model;
        cpu.Cores = cores;
        cpu.Threads = threads;

        await repo.UpdateAsync(resource);
    }
}