using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.Services;

public class CloneServiceUseCase(IServiceRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string originalName, string cloneName)
    {
        originalName = Normalize.ServiceName(originalName);
        ThrowIfInvalid.ResourceName(originalName);
        
        cloneName = Normalize.ServiceName(cloneName);
        ThrowIfInvalid.ResourceName(cloneName);

        var existingResourceKind = await resourceRepo.GetResourceKindAsync(cloneName);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{cloneName}' already exists.");
        
        var original = await repository.GetByNameAsync(originalName);
        if (original == null)
        {
            throw new NotFoundException($"Resource '{originalName}' not found.");
        }

        var clone = Clone.DeepClone(original);
        clone.Name = cloneName;
        
        await repository.AddAsync(clone);
    }
}