using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.UseCases;

public interface IRenameResourceUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(string originalName, string newName);
}

public class RenameResourceUseCase<T>(IResourceCollection repo) : IRenameResourceUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string originalName, string newName)
    {
        originalName = Normalize.SystemName(originalName);
        ThrowIfInvalid.ResourceName(originalName);
        
        newName = Normalize.SystemName(newName);
        ThrowIfInvalid.ResourceName(newName);

        var existingResource = await repo.GetByNameAsync(newName);
        if (existingResource != null)
            throw new ConflictException($"{existingResource.Kind} resource '{newName}' already exists.");
        
        var original = await repo.GetByNameAsync(originalName);
        if (original == null)
        {
            throw new NotFoundException($"Resource '{originalName}' not found.");
        }

        original.Name = newName;
        await repo.UpdateAsync(original);
        
        var children = await repo.GetDependantsAsync(originalName);
        foreach (var child in children)
        {
            child.RunsOn = newName;
            await repo.UpdateAsync(child);
        }
    }
}