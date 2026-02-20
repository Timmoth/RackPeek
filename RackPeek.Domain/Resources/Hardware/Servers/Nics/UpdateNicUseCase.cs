using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.Servers.Nics;

public class UpdateNicUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int index,
        string? type,
        double? speed,
        int? ports)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        if (speed.HasValue) ThrowIfInvalid.NicSpeed(speed.Value);
        if (ports.HasValue) ThrowIfInvalid.NicPorts(ports.Value);

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