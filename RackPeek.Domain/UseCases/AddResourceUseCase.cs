using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases;

public interface IAddResourceUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    Task ExecuteAsync(string name, List<string>? runsOn = null);
}

public class AddResourceUseCase<T>(IResourceCollection repo) : IAddResourceUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string name, List<string>? runsOn = null)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var existingResource = await repo.GetByNameAsync(name);
        if (existingResource != null)
            throw new ConflictException($"Resource '{name}' ({existingResource.Kind}) already exists.");

        if (runsOn != null)
        {
            foreach (var parent in runsOn) {
                var normalizedParent = Normalize.HardwareName(parent);
                ThrowIfInvalid.ResourceName(normalizedParent);
                var parentResource = await repo.GetByNameAsync(normalizedParent);
                if (parentResource == null) throw new NotFoundException($"Resource '{normalizedParent}' not found.");

                if (!Resource.CanRunOn<T>(parentResource))
                    throw new InvalidOperationException(
                        $" {Resource.GetKind<T>()} cannot run on {parentResource.Kind} '{normalizedParent}'.");
            }
        }

        var resource = Activator.CreateInstance<T>();
        resource.Name = name;
        resource.RunsOn = runsOn ?? new List<string>();

        await repo.AddAsync(resource);
    }
}
