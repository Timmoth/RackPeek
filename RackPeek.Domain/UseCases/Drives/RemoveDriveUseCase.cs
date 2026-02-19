using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.UseCases.Drives;

public interface IRemoveDriveUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(string name, int index);
}


    
public class RemoveDriveUseCase<T>(IResourceCollection repository) : IRemoveDriveUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var resource = await repository.GetByNameAsync(name) ?? throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IDriveResource dr)
        {
            throw new NotFoundException($"Resource '{name}' not found.");
        }

        if (dr.Drives == null || index < 0 || index >= dr.Drives.Count)
            throw new NotFoundException($"Drive index {index} not found on '{name}'.");

        dr.Drives.RemoveAt(index);

        await repository.UpdateAsync(resource);
    }
}