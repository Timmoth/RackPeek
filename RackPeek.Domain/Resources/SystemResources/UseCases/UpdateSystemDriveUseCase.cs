using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class UpdateSystemDriveUseCase(ISystemRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string systemName, int index, string driveType, int size)
    {
        ThrowIfInvalid.ResourceName(systemName);
        var driveTypeNormalized = Normalize.DriveType(driveType);
        ThrowIfInvalid.DriveType(driveTypeNormalized);
        ThrowIfInvalid.DriveSize(size);

        var system = await repository.GetByNameAsync(systemName) ??
                     throw new NotFoundException($"System '{systemName}' not found.");

        if (system.Drives == null || index < 0 || index >= system.Drives.Count)
            throw new NotFoundException($"Drive index {index} not found on system '{systemName}'.");

        var drive = system.Drives[index];

        drive.Type = driveTypeNormalized;
        drive.Size = size;

        await repository.UpdateAsync(system);
    }
}