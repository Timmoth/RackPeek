using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class UpdateServerUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int? ramGb = null,
        bool? ipmi = null
    )
    {
        ThrowIfInvalid.ResourceName(name);

        var server = await repository.GetByNameAsync(name) as Server;
        if (server == null)
            throw new NotFoundException($"Server '{name}' not found.");

        // ---- RAM ----
        if (ramGb.HasValue)
        {
            ThrowIfInvalid.RamGb(ramGb);
            server.Ram ??= new Ram();
            server.Ram.Size = ramGb.Value;
        }

        // ---- IPMI ----
        if (ipmi.HasValue) server.Ipmi = ipmi.Value;

        await repository.UpdateAsync(server);
    }
}