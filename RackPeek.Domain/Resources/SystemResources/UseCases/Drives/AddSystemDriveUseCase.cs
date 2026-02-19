using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class AddSystemDriveUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string systemName, string driveType, int size)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        ThrowIfInvalid.ResourceName(systemName);

        var driveTypeNormalized = Normalize.DriveType(driveType);
        ThrowIfInvalid.DriveType(driveTypeNormalized);
        ThrowIfInvalid.DriveSize(size);

        var system = (await repository.GetByNameAsync(systemName)) as SystemResource
                     ?? throw new NotFoundException($"System '{systemName}' not found.");

        system.Drives ??= new List<Drive>();

        system.Drives.Add(new Drive
        {
            Type = driveTypeNormalized,
            Size = size
        });

        await repository.UpdateAsync(system);
    }
}