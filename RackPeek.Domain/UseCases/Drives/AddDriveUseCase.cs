using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.UseCases.Drives;

public interface IAddDriveUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        string? type,
        int? size);
}

public class AddDriveUseCase<T>(IResourceCollection repository) : IAddDriveUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(
        string name,
        string? type,
        int? size)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var resource = await repository.GetByNameAsync<T>(name) ??
                       throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IDriveResource dr) throw new NotFoundException($"Resource '{name}' not found.");

        dr.Drives ??= new List<Drive>();
        dr.Drives.Add(new Drive
        {
            Type = type,
            Size = size
        });

        await repository.UpdateAsync(resource);
    }
}