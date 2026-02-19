using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public class AddDesktopNicUseCase(IResourceCollection repository) : IUseCase
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

        var nicType = Normalize.NicType(type);
        ThrowIfInvalid.NicType(nicType);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        desktop.Nics ??= new List<Nic>();
        desktop.Nics.Add(new Nic
        {
            Type = nicType,
            Speed = speed,
            Ports = ports
        });
        await repository.UpdateAsync(desktop);
    }
}