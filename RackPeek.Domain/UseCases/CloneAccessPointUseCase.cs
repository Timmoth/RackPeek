using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases;

public interface ICloneResourceUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(string originalName, string cloneName);
}


public class CloneResourceUseCase<T>(IResourceCollection repo) : ICloneResourceUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string originalName, string cloneName)
    {
        originalName = Normalize.HardwareName(originalName);
        ThrowIfInvalid.ResourceName(originalName);

        cloneName = Normalize.HardwareName(cloneName);
        ThrowIfInvalid.ResourceName(cloneName);
        
        var resource = await repo.GetByNameAsync(cloneName);
        if (resource != null)
            throw new ConflictException($"{resource.Kind} resource '{cloneName}' already exists.");

        var original = await repo.GetByNameAsync(originalName) as T;
        if (original == null)
        {
            throw new NotFoundException($"Resource '{originalName}' not found.");
        }
        
        var clone = Clone.DeepClone(original);
        clone.Name = cloneName;
        
        await repo.AddAsync(clone);
    }
}