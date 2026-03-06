using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.UseCases.Drives;

public interface IUpdateDriveUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(string name, int index, string? type, int? size);
}

public class UpdateDriveUseCase<T>(IResourceCollection repository) : IUpdateDriveUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(string name, int index, string? type, int? size)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        T resource = await repository.GetByNameAsync<T>(name) ??
                     throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IDriveResource dr) throw new NotFoundException($"Resource '{name}' not found.");

        if (dr.Drives == null || index < 0 || index >= dr.Drives.Count)
            throw new NotFoundException($"Drive index {index} not found on '{name}'.");

        Drive drive = dr.Drives[index];
        drive.Type = type;
        drive.Size = size;

        await repository.UpdateAsync(resource);
    }
}
