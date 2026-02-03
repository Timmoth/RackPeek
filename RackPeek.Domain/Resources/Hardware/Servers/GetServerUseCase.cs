using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class GetServerUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Server> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Server server)
        {
            throw new NotFoundException($"Server '{name}' not found.");
        }

        return server;
    }
}