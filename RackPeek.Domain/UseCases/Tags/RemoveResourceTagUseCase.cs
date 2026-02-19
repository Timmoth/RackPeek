using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.UseCases.Tags;
public interface IRemoveTagUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    Task ExecuteAsync(string name, string tag);
}

public class RemoveTagUseCase<T>(IResourceCollection repo)
    : IRemoveTagUseCase<T>
    where T : Resource
{
    public async Task ExecuteAsync(string name, string tag)
    {
        tag = Normalize.Tag(tag);

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var resource = await repo.GetByNameAsync(name)
                       ?? throw new NotFoundException($"Resource '{name}' not found.");

        if (resource.Tags is null || resource.Tags.Length == 0)
            return;

        var updated = resource.Tags
            .Where(t => t != tag)
            .ToArray();

        if (updated.Length == resource.Tags.Length)
            return; // tag didn't exist

        resource.Tags = updated.Length == 0 ? null : updated;

        await repo.UpdateAsync(resource);
    }
}
