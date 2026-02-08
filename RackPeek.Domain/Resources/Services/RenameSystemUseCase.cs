using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.Services;

public class RenameServiceUseCase(IServiceRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string originalName, string newName)
    {
        originalName = Normalize.ServiceName(originalName);
        ThrowIfInvalid.ResourceName(originalName);
        
        newName = Normalize.ServiceName(newName);
        ThrowIfInvalid.ResourceName(newName);

        var existingResourceKind = await resourceRepo.GetResourceKindAsync(newName);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{newName}' already exists.");
        
        var original = await repository.GetByNameAsync(originalName);
        if (original == null)
        {
            throw new NotFoundException($"Resource '{originalName}' not found.");
        }
        
        original.Name = newName;
        
        await repository.UpdateAsync(original);
    }
}