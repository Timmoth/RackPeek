using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Servers;

namespace RackPeek.Domain.UseCases.Gpus;

public interface IRemoveGpuUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(string name, int index);
}

public class RemoveGpuUseCase<T>(IResourceCollection repository) : IRemoveGpuUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var resource = await repository.GetByNameAsync<T>(name) ??
                       throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IGpuResource gr) throw new NotFoundException($"Resource '{name}' not found.");

        if (gr.Gpus == null || index < 0 || index >= gr.Gpus.Count)
            throw new NotFoundException($"GPU index {index} not found on '{name}'.");

        gr.Gpus.RemoveAt(index);

        await repository.UpdateAsync(resource);
    }
}