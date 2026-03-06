using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.OtherHardware;

public class UpdateOtherUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        string? description = null,
        string? notes = null
    )
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var other = await repository.GetByNameAsync(name) as Other;
        if (other == null)
            throw new InvalidOperationException($"Other hardware '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            other.Model = model;

        if (description != null)
            other.Description = description;

        if (notes != null) other.Notes = notes;
        await repository.UpdateAsync(other);
    }
}
