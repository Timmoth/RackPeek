using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Connections;

namespace RackPeek.Domain.UseCases;

public interface IRenameResourceUseCase<T> : IResourceUseCase<T>
    where T : Resource {
    Task ExecuteAsync(string originalName, string newName);
}

public class RenameResourceUseCase<T>(IResourceCollection repo) : IRenameResourceUseCase<T> where T : Resource {
    public async Task ExecuteAsync(string originalName, string newName) {
        originalName = Normalize.HardwareName(originalName);
        ThrowIfInvalid.ResourceName(originalName);

        newName = Normalize.HardwareName(newName);
        ThrowIfInvalid.ResourceName(newName);

        Resource? existingResource = await repo.GetByNameAsync(newName);
        if (existingResource != null)
            throw new ConflictException($"{existingResource.Kind} resource '{newName}' already exists.");

        Resource? original = await repo.GetByNameAsync(originalName);
        if (original == null)
            throw new NotFoundException($"Resource '{originalName}' not found.");

        original.Name = newName;

        // A person has now chosen this name, so discovery must stop improving on it.
        original.UserNamed = true;

        await repo.UpdateAsync(original);

        IReadOnlyList<Resource> allResources = await repo.GetAllOfTypeAsync<Resource>();

        foreach (Resource resource in allResources) {
            if (resource.RunsOn.Contains(originalName, StringComparer.OrdinalIgnoreCase)) {
                resource.RunsOn = resource.RunsOn
                    .ConvertAll(p => p.Equals(originalName, StringComparison.OrdinalIgnoreCase) ? newName : p);

                await repo.UpdateAsync(resource);
            }
        }

        IReadOnlyList<Connection> connections = await repo.GetConnectionsAsync();
        foreach (Connection connection in connections) {
            var updated = false;

            if (connection.A.Resource.Equals(originalName, StringComparison.OrdinalIgnoreCase)) {
                connection.A.Resource = newName;
                updated = true;
            }

            if (connection.B.Resource.Equals(originalName, StringComparison.OrdinalIgnoreCase)) {
                connection.B.Resource = newName;
                updated = true;
            }

            if (updated) {
                await repo.RemoveConnectionAsync(connection);
                await repo.AddConnectionAsync(connection);
            }
        }
    }
}
