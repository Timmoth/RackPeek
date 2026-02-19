using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Switches.Ports;

public class UpdateSwitchPortUseCase(IResourceCollection repository) : IUseCase
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

        var Switch = await repository.GetByNameAsync(name) as Switch
                     ?? throw new NotFoundException($"Switch '{name}' not found.");

        if (Switch.Ports == null || index < 0 || index >= Switch.Ports.Count)
            throw new NotFoundException($"Port index {index} not found on Switch '{name}'.");

        var nic = Switch.Ports[index];
        nic.Type = nicType;
        nic.Speed = speed;
        nic.Count = ports;

        await repository.UpdateAsync(Switch);
    }
}