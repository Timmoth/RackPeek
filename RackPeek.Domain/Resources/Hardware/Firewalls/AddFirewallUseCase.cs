using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Firewalls;

public class AddFirewallUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        // basic guard rails
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new NotFoundException($"Firewall '{name}' already exists.");

        var firewallResource = new Firewall
        {
            Name = name
        };

        await repository.AddAsync(firewallResource);
    }
}