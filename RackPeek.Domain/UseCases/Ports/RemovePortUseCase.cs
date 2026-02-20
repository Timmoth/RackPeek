using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace RackPeek.Domain.UseCases.Ports;

public interface IRemovePortUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(string name, int index);
}

public class RemovePortUseCase<T>(IResourceCollection repository) : IRemovePortUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var resource = await repository.GetByNameAsync<T>(name)
                       ?? throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IPortResource pr) throw new NotFoundException($"Resource '{name}' not found.");


        if (pr.Ports == null || index < 0 || index >= pr.Ports.Count)
            throw new NotFoundException($"Port index {index} not found on '{name}'.");

        pr.Ports.RemoveAt(index);

        await repository.UpdateAsync(resource);
    }
}