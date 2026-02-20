using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace RackPeek.Domain.UseCases.Cpus;

public interface IRemoveCpuUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        int index);
}

public class RemoveCpuUseCase<T>(IResourceCollection repo) : IRemoveCpuUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(
        string name,
        int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var resource = await repo.GetByNameAsync<T>(name) ??
                       throw new NotFoundException($"Resource '{name}' not found.");
        if (resource is not ICpuResource cpuResource) return;

        cpuResource.Cpus ??= [];

        if (index < 0 || index >= cpuResource.Cpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "CPU index out of range.");

        cpuResource.Cpus.RemoveAt(index);

        await repo.UpdateAsync(resource);
    }
}