using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class RemoveSystemDriveUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string systemName, int index)
    {
        ThrowIfInvalid.ResourceName(systemName);

        var system = await repository.GetByNameAsync(systemName) as SystemResource
                     ?? throw new NotFoundException($"System '{systemName}' not found.");

        if (system.Drives == null || index < 0 || index >= system.Drives.Count)
            throw new NotFoundException($"Drive index {index} not found on system '{systemName}'.");

        system.Drives.RemoveAt(index);

        await repository.UpdateAsync(system);
    }
}