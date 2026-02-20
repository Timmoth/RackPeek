using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Firewalls;
using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.UseCases.Ports;
public interface IAddPortUseCase<T> : IResourceUseCase<T>
    where T : Resource
{
    public Task ExecuteAsync(
        string name,
        string? type,
        double? speed,
        int? ports);
}

public class AddPortUseCase<T>(IResourceCollection repository) : IAddPortUseCase<T> where T : Resource
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

        var resource = await repository.GetByNameAsync<T>(name)
                      ?? throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IPortResource pr)
        {
            throw new NotFoundException($"Resource '{name}' not found.");
        }
        
        pr.Ports ??= new List<Port>();
        pr.Ports.Add(new Port
        {
            Type = nicType,
            Speed = speed,
            Count = ports
        });
        await repository.UpdateAsync(resource);
    }
}