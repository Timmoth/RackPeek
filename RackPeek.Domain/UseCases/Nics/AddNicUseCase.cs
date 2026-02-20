using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public interface IAddNicUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        string? type,
        double? speed,
        int? ports);
}

public class AddNicUseCase<T>(IResourceCollection repository) : IAddNicUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(
        string name,
        string? type,
        double? speed,
        int? ports)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var nicType = Normalize.NicType(type);
        ThrowIfInvalid.NicType(nicType);

        var resource = await repository.GetByNameAsync<T>(name) ??
                       throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not INicResource nr) throw new NotFoundException($"Resource '{name}' not found.");

        nr.Nics ??= new List<Nic>();
        nr.Nics.Add(new Nic
        {
            Type = nicType,
            Speed = speed,
            Ports = ports
        });
        await repository.UpdateAsync(resource);
    }
}