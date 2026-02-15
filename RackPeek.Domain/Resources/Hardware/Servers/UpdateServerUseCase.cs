using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class UpdateServerUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        bool? ipmi = null,
        string? notes = null
    )
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var server = await repository.GetByNameAsync(name) as Server;
        if (server == null)
            throw new NotFoundException($"Server '{name}' not found.");

        if (ipmi.HasValue) server.Ipmi = ipmi.Value;
        if (notes != null)
        {
            server.Notes = notes;
        }
        await repository.UpdateAsync(server);
    }
}