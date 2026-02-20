using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.UseCases.Cpus;

public interface IAddCpuUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        string? model,
        int? cores,
        int? threads);
}


    
public class AddCpuUseCase<T>(IResourceCollection repo) : IAddCpuUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(
        string name,
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

        cpuResource.Cpus.Add(new Cpu
        {
            Model = model,
            Cores = cores,
            Threads = threads
        });

        await repo.UpdateAsync(resource);
    }
}