using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.UseCases.Gpus;

public interface IAddGpuUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        string? model,
        int? vram);
}

public class AddGpuUseCase<T>(IResourceCollection repository) : IAddGpuUseCase<T> where T : Resource
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

        var resource = await repository.GetByNameAsync<T>(name) ??
                       throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IGpuResource gr) throw new NotFoundException($"Resource '{name}' not found.");

        gr.Gpus ??= new List<Gpu>();
        gr.Gpus.Add(new Gpu
        {
            Model = model,
            Vram = vram
        });
        await repository.UpdateAsync(resource);
    }
}