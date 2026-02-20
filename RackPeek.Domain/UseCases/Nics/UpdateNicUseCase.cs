using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public interface IUpdateNicUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        int index,
        string? type,
        double? speed,
        int? ports);
}

public class UpdateNicUseCase<T>(IResourceCollection repository) : IUpdateNicUseCase<T> where T : Resource
{
    public async Task ExecuteAsync(
        string name,
        int index,
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

        if (nr.Nics == null || index < 0 || index >= nr.Nics.Count)
            throw new NotFoundException($"NIC index {index} not found on desktop '{name}'.");

        var nic = nr.Nics[index];
        nic.Type = nicType;
        nic.Speed = speed;
        nic.Ports = ports;

        await repository.UpdateAsync(resource);
    }
}