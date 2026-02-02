using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Firewalls;

public class GetFirewallUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Firewall?> ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        return hardware as Firewall;
    }
}