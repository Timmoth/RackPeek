using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Servers;

namespace RackPeek.Domain.UseCases.Nics;

public interface IRemoveNicUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(string name, int index);
}

public class RemoveNicUseCase<T>(IResourceCollection repository) : IRemoveNicUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var resource = await repository.GetByNameAsync<T>(name) ??
                       throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not INicResource nr) throw new NotFoundException($"Resource '{name}' not found.");

        if (nr.Nics == null || index < 0 || index >= nr.Nics.Count)
            throw new NotFoundException($"NIC index {index} not found on desktop '{name}'.");

        nr.Nics.RemoveAt(index);

        await repository.UpdateAsync(resource);
    }
}