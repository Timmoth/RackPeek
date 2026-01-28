using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Nics;

public class UpdateNicUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string serverName,
        int index,
        string type,
        int speed,
        int ports)
    {
        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Server server)
            return;

        server.Nics ??= [];

        if (index < 0 || index >= server.Nics.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "NIC index out of range.");

        var nic = server.Nics[index];
        nic.Type = type;
        nic.Speed = speed;
        nic.Ports = ports;

        await repository.UpdateAsync(server);
    }
}