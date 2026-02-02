using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class UpdateSystemDriveUseCase(ISystemRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string systemName, string type, int size)
    {
        ThrowIfInvalid.ResourceName(systemName);
        ThrowIfInvalid.ResourceName(type);

        if (size < 0)
            throw new ValidationException("Drive size must be a non‑negative number of gigabytes.");

        var system = await repository.GetByNameAsync(systemName)
                     ?? throw new NotFoundException($"System '{systemName}' not found.");

        var drive = system.Drives?.FirstOrDefault(d => d.Type == type)
                    ?? throw new NotFoundException($"Drive '{type}' not found on system '{systemName}'.");

        drive.Size = size;

        await repository.UpdateAsync(system);
    }
}