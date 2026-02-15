using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Ram;

public class SetServerRamUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int? ramGb = null,
        int? ramMts = null)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server)
            throw new NotFoundException($"Server '{name}' not found.");

        if (ramGb.HasValue)
        {
            ThrowIfInvalid.RamGb(ramGb);
            server.Ram ??= new Models.Ram();
            server.Ram.Size = ramGb.Value;
        }

        if (ramMts.HasValue)
        {
            server.Ram ??= new Models.Ram();
            server.Ram.Mts = ramMts.Value;
        }

        await repository.UpdateAsync(server);
    }
}
