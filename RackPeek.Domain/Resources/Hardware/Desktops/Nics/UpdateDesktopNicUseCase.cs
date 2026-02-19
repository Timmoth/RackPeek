using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public class UpdateDesktopNicUseCase(IResourceCollection repository) : IUseCase
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

        var nicType = Normalize.NicType(type);
        ThrowIfInvalid.NicType(nicType);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        if (desktop.Nics == null || index < 0 || index >= desktop.Nics.Count)
            throw new NotFoundException($"NIC index {index} not found on desktop '{name}'.");

        var nic = desktop.Nics[index];
        nic.Type = nicType;
        nic.Speed = speed;
        nic.Ports = ports;

        await repository.UpdateAsync(desktop);
    }
}