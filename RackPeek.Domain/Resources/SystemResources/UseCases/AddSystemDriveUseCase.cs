using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class AddSystemDriveUseCase(ISystemRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string systemName, string driveType, int size)
    {
        ThrowIfInvalid.ResourceName(systemName);
        
        var driveTypeNormalized = Normalize.DriveType(driveType);
        ThrowIfInvalid.DriveType(driveTypeNormalized);
        ThrowIfInvalid.DriveSize(size); 

        var system = await repository.GetByNameAsync(systemName)
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