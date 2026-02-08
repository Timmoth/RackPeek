using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class RenameSystemUseCase(ISystemRepository repository, IServiceRepository serviceRepo, IResourceRepository resourceRepo) : IUseCase
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
        
        var original = await repository.GetByNameAsync(originalName);
        if (original == null)
        {
            throw new NotFoundException($"Resource '{originalName}' not found.");
        }

        original.Name = newName;
        await repository.UpdateAsync(original);
        
        var children = await serviceRepo.GetBySystemHostAsync(originalName);
        foreach (var child in children)
        {
            child.RunsOn = newName;
            await serviceRepo.UpdateAsync(child);
        }
    }
}