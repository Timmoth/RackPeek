using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Nics;

public class AddNicUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string serverName,
        string type,
        int speed,
        int ports)
    {
        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Server server)
            return;

        server.Nics ??= [];

        server.Nics.Add(new Nic
        {
            Type = type,
            Speed = speed,
            Ports = ports
        });

        await repository.UpdateAsync(server);
    }
}