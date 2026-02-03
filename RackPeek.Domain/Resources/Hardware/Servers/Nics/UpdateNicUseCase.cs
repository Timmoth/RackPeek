using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Nics;

public class UpdateNicUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int index,
        string type,
        int speed,
        int ports)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        ThrowIfInvalid.NicSpeed(speed);
        ThrowIfInvalid.NicPorts(ports);

        var nicType = Normalize.NicType(type);
        ThrowIfInvalid.NicType(nicType);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server)
            throw new NotFoundException($"Server: '{name}' not found.");

        server.Nics ??= [];

        if (index < 0 || index >= server.Nics.Count)
            throw new ValidationException("NIC index out of range.");

        var nic = server.Nics[index];
        nic.Type = nicType;
        nic.Speed = speed;
        nic.Ports = ports;

        await repository.UpdateAsync(server);
    }
}