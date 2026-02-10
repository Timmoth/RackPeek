using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class CloneAccessPointUseCase(IHardwareRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string originalName, string cloneName)
    {
        originalName = Normalize.HardwareName(originalName);
        ThrowIfInvalid.ResourceName(originalName);

        cloneName = Normalize.HardwareName(cloneName);
        ThrowIfInvalid.ResourceName(cloneName);
        
        var existingResourceKind = await resourceRepo.GetResourceKindAsync(cloneName);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{cloneName}' already exists.");

        var original = await repository.GetByNameAsync(originalName) as AccessPoint;
        if (original == null)
        {
            throw new NotFoundException($"Resource '{originalName}' not found.");
        }
        
        var clone = Clone.DeepClone(original);
        clone.Name = cloneName;
        
        await repository.AddAsync(clone);
    }
}