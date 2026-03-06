using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Labels;

/// <summary>
///     Removes a label by key from a resource.
/// </summary>
public interface IRemoveLabelUseCase<T> : IResourceUseCase<T>
    where T : Resource {
    /// <summary>
    ///     Removes the label with the given key. No-op if the key does not exist.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="key">Label key to remove.</param>
    /// <exception cref="NotFoundException">Thrown when the resource does not exist.</exception>
    /// <exception cref="ValidationException">Thrown when key fails validation.</exception>
    Task ExecuteAsync(string name, string key);
}

/// <summary>
///     Removes a label by key from a resource.
/// </summary>
public class RemoveLabelUseCase<T>(IResourceCollection repo) : IRemoveLabelUseCase<T>
    where T : Resource {
    /// <inheritdoc />
    public async Task ExecuteAsync(string name, string key) {
        key = Normalize.LabelKey(key);

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        ThrowIfInvalid.LabelKey(key);

        Resource? resource = await repo.GetByNameAsync(name);
        if (resource is null)
            throw new NotFoundException($"Resource '{name}' not found.");

        if (!resource.Labels.Remove(key))
            return;

        await repo.UpdateAsync(resource);
    }
}
