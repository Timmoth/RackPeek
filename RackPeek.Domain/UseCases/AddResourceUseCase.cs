using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.UseCases;

public interface IAddResourceUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    Task ExecuteAsync(string name, string? runsOn = null);
}


public class AddResourceUseCase<T>(IResourceCollection repo) : IAddResourceUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string name, string? runsOn = null)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        
        var existingResource = await repo.GetByNameAsync(name);
        if (existingResource != null)
            throw new ConflictException($"{existingResource.Kind} resource '{name}' already exists.");

        if (runsOn != null)
        {
            runsOn = Normalize.HardwareName(runsOn);
            ThrowIfInvalid.ResourceName(runsOn);
            var parentResource = await repo.GetByNameAsync(runsOn);
            if (parentResource == null)
            {
                throw new NotFoundException($"Resource '{runsOn}' not found.");
            }

            if (!Resource.CanRunOn<T>(parentResource))
            {
                throw new InvalidOperationException($" {Resource.GetKind<T>()} cannot run on {parentResource.Kind} '{runsOn}'.");
            }
        }
        
        var resource = Activator.CreateInstance<T>();
        resource.Name = name;
        resource.RunsOn = runsOn;

        await repo.AddAsync(resource);
    }
}