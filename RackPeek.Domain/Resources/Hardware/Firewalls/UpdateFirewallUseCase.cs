using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Firewalls;

public class UpdateFirewallUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        bool? managed = null,
        bool? poe = null
    )
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var firewallResource = await repository.GetByNameAsync(name) as Firewall;
        if (firewallResource == null)
            throw new InvalidOperationException($"Firewall '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            firewallResource.Model = model;

        if (managed.HasValue)
            firewallResource.Managed = managed.Value;

        if (poe.HasValue)
            firewallResource.Poe = poe.Value;

        await repository.UpdateAsync(firewallResource);
    }
}