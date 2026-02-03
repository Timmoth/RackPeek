using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Firewalls;

public class GetFirewallUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Firewall> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Firewall firewall)
        {
            throw new NotFoundException($"Firewall '{name}' not found.");
        }
        
        return firewall;
    }
}