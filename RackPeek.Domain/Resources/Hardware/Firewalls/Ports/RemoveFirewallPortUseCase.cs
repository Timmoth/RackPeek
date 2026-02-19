using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Firewalls.Ports;

public class RemoveFirewallPortUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var firewall = await repository.GetByNameAsync(name) as Firewall
                       ?? throw new NotFoundException($"Firewall '{name}' not found.");

        if (firewall.Ports == null || index < 0 || index >= firewall.Ports.Count)
            throw new NotFoundException($"Port index {index} not found on firewall '{name}'.");

        firewall.Ports.RemoveAt(index);

        await repository.UpdateAsync(firewall);
    }
}