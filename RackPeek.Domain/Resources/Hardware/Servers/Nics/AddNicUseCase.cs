using RackPeek.Domain.Helpers;
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
        ThrowIfInvalid.ResourceName(serverName);
        ThrowIfInvalid.NicSpeed(speed);
        ThrowIfInvalid.NicPorts(ports);

        var nicType = Normalize.NicType(type);
        ThrowIfInvalid.NicType(nicType);

        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Server server)
            throw new NotFoundException($"Server: '{serverName}' not found.");

        server.Nics ??= [];

        server.Nics.Add(new Nic
        {
            Type = nicType,
            Speed = speed,
            Ports = ports
        });

        await repository.UpdateAsync(server);
    }
}