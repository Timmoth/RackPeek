using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Tags;

public interface IAddTagUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    Task ExecuteAsync(string name, string tag);
}

public class AddTagUseCase<T>(IResourceCollection repo) : IAddTagUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string name, string tag)
    {
        tag = Normalize.Tag(tag);

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var resource = await repo.GetByNameAsync(name);
        if (resource == null)
            throw new NotFoundException($"Resource '{name}' not found.");

        if (resource.Tags == null)
            resource.Tags = [tag];
        else if (!resource.Tags.Contains(tag))
            resource.Tags = [..resource.Tags, tag];
        else
            // Tag already exists
            return;

        await repo.UpdateAsync(resource);
    }
}