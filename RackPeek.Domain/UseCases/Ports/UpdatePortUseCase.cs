using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Firewalls;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace RackPeek.Domain.UseCases.Ports;
public interface IUpdatePortUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        int index,
        string? type,
        double? speed,
        int? ports);
}

public class UpdatePortUseCase<T>(IResourceCollection repository) : IUpdatePortUseCase<T> where T : Resource
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

        var resource = await repository.GetByNameAsync<T>(name)
                       ?? throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IPortResource pr)
        {
            throw new NotFoundException($"Resource '{name}' not found.");
        }

        if (pr.Ports == null || index < 0 || index >= pr.Ports.Count)
            throw new NotFoundException($"Port index {index} not found on '{name}'.");

        var nic = pr.Ports[index];
        nic.Type = nicType;
        nic.Speed = speed;
        nic.Count = ports;

        await repository.UpdateAsync(resource);
    }
}