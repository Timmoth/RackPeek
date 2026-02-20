using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace RackPeek.Domain.UseCases.Gpus;

public interface IUpdateGpuUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        int index,
        string? model,
        int? vram);
}

public class UpdateGpuUseCase<T>(IResourceCollection repository) : IUpdateGpuUseCase<T> where T : Resource
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

        var resource = await repository.GetByNameAsync<T>(name) ??
                       throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IGpuResource gr) throw new NotFoundException($"Resource '{name}' not found.");

        if (gr.Gpus == null || index < 0 || index >= gr.Gpus.Count)
            throw new NotFoundException($"GPU index {index} not found on '{name}'.");

        var gpu = gr.Gpus[index];
        gpu.Model = model;
        gpu.Vram = vram;
        await repository.UpdateAsync(resource);
    }
}