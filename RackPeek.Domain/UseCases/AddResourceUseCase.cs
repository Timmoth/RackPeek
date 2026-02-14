using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.UseCases;

public interface IAddResourceUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    Task ExecuteAsync(string name);
}


public class AddResourceUseCase<T>(IResourceRepo<T> repo, IResourceRepository resourceRepository) : IAddResourceUseCase<T> where T : Resource, new()
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var existingResourceKind = await resourceRepository.GetResourceKindAsync(name);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{name}' already exists.");
        
        var resource = new T
        {
            Name = name
        };
        await repo.AddAsync(resource);
    }
}