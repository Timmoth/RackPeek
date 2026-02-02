using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class RemoveSystemDriveUseCase(ISystemRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string systemName, string type)
    {
        ThrowIfInvalid.ResourceName(systemName);
        ThrowIfInvalid.ResourceName(type);

        var system = await repository.GetByNameAsync(systemName)
                     ?? throw new NotFoundException($"System '{systemName}' not found.");

        var drive = system.Drives?.FirstOrDefault(d => d.Type == type)
                    ?? throw new NotFoundException($"Drive '{type}' not found on system '{systemName}'.");

        system.Drives!.Remove(drive);

        await repository.UpdateAsync(system);
    }
}