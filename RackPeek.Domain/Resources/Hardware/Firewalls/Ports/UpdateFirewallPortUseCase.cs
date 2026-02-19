using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Firewalls.Ports;

public class UpdateFirewallPortUseCase(IResourceCollection repository) : IUseCase
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

        var firewall = await repository.GetByNameAsync(name) as Firewall
                       ?? throw new NotFoundException($"Firewall '{name}' not found.");

        if (firewall.Ports == null || index < 0 || index >= firewall.Ports.Count)
            throw new NotFoundException($"Port index {index} not found on firewall '{name}'.");

        var nic = firewall.Ports[index];
        nic.Type = nicType;
        nic.Speed = speed;
        nic.Count = ports;

        await repository.UpdateAsync(firewall);
    }
}