using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class AddSystemUseCase(ISystemRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);

        var existingResourceKind = await resourceRepo.GetResourceKindAsync(name);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{name}' already exists.");

        var system = new SystemResource
        {
            Name = name
        };

        await repository.AddAsync(system);
    }
}