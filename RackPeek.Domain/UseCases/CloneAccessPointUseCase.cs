using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases;

public interface ICloneResourceUseCase<T> : IResourceUseCase<T>
    where T : Resource {
    public Task ExecuteAsync(string originalName, string cloneName);
}

public class CloneResourceUseCase<T>(IResourceCollection repo) : ICloneResourceUseCase<T> where T : Resource {
    public async Task ExecuteAsync(string originalName, string cloneName) {
        originalName = Normalize.HardwareName(originalName);
        ThrowIfInvalid.ResourceName(originalName);

        cloneName = Normalize.HardwareName(cloneName);
        ThrowIfInvalid.ResourceName(cloneName);

        Resource? resource = await repo.GetByNameAsync(cloneName);
        if (resource != null)
            throw new ConflictException($"{resource.Kind} resource '{cloneName}' already exists.");

        var original = await repo.GetByNameAsync(originalName) as T;
        if (original == null) throw new NotFoundException($"Resource '{originalName}' not found.");

        T clone = Clone.DeepClone(original);
        clone.Name = cloneName;

        // A discoveryId names one machine; a copy of its card is not that machine.
        // Keeping it would also put two resources with the same id in the store,
        // which DiscoveryIdResolver rejects on every subsequent discovery import.
        clone.DiscoveryId = null;

        await repo.AddAsync(clone);
    }
}
