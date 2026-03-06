using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Labels;

/// <summary>
///     Adds or updates a key-value label on a resource.
/// </summary>
public interface IAddLabelUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    /// <summary>
    ///     Adds or overwrites a label on the resource. If the key already exists, the value is updated.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="key">Label key.</param>
    /// <param name="value">Label value.</param>
    /// <exception cref="NotFoundException">Thrown when the resource does not exist.</exception>
    /// <exception cref="ValidationException">Thrown when key or value fails validation.</exception>
    Task ExecuteAsync(string name, string key, string value);
}

/// <summary>
///     Adds or updates a key-value label on a resource.
/// </summary>
public class AddLabelUseCase<T>(IResourceCollection repo) : IAddLabelUseCase<T>
    where T : Resource
{
    /// <inheritdoc />
    public async Task ExecuteAsync(string name, string key, string value)
    {
        key = Normalize.LabelKey(key);
        value = Normalize.LabelValue(value);

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        ThrowIfInvalid.LabelKey(key);
        ThrowIfInvalid.LabelValue(value);

        Resource? resource = await repo.GetByNameAsync(name);
        if (resource is null)
            throw new NotFoundException($"Resource '{name}' not found.");

        resource.Labels[key] = value;

        await repo.UpdateAsync(resource);
    }
}
