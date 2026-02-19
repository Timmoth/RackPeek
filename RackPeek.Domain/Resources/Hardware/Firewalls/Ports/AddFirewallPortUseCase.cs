using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Firewalls.Ports;

public class AddFirewallPortUseCase(IResourceCollection repository) : IUseCase
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

        var desktop = await repository.GetByNameAsync(name) as Firewall
                      ?? throw new NotFoundException($"Firewall '{name}' not found.");

        desktop.Ports ??= new List<Port>();
        desktop.Ports.Add(new Port
        {
            Type = nicType,
            Speed = speed,
            Count = ports
        });
        await repository.UpdateAsync(desktop);
    }
}