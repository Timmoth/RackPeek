using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases;

public interface IDeleteResourceUseCase<T> : IResourceUseCase<T>
    where T : Resource {
    Task ExecuteAsync(string name);
}

public class DeleteResourceUseCase<T>(IResourceCollection repo) : IDeleteResourceUseCase<T> where T : Resource {
    public async Task ExecuteAsync(string name) {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        Resource? existingResource = await repo.GetByNameAsync(name);
        if (existingResource == null)
            throw new NotFoundException($"Resource '{name}' does not exist.");

        IReadOnlyList<Resource> dependants = await repo.GetDependantsAsync(name);
        foreach (Resource resource in dependants) {
            resource.RunsOn.Remove(name);
            await repo.UpdateAsync(resource);
        }

        await repo.DeleteAsync(name);
    }
}
