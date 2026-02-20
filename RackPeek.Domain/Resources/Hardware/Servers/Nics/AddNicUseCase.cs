using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Hardware.Servers.Nics;

public class AddNicUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
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

        server.Nics.Add(new Nic
        {
            Type = nicType,
            Speed = speed,
            Ports = ports
        });

        await repository.UpdateAsync(server);
    }
}