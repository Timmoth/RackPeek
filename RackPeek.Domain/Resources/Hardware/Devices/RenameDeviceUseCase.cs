using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware.Devices;

public class RenameDeviceUseCase(IHardwareRepository repository, ISystemRepository systemRepo, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string originalName, string newName)
    {
        originalName = Normalize.SystemName(originalName);
        ThrowIfInvalid.ResourceName(originalName);

        newName = Normalize.SystemName(newName);
        ThrowIfInvalid.ResourceName(newName);

        var existingResourceKind = await resourceRepo.GetResourceKindAsync(newName);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{newName}' already exists.");

        var original = await repository.GetByNameAsync(originalName) as Device;
        if (original == null)
            throw new NotFoundException($"Resource '{originalName}' not found.");

        original.Name = newName;
        await repository.UpdateAsync(original);

        var children = await systemRepo.GetByPhysicalHostAsync(originalName);
        foreach (var child in children)
        {
            child.RunsOn = newName;
            await systemRepo.UpdateAsync(child);
        }
    }
}
