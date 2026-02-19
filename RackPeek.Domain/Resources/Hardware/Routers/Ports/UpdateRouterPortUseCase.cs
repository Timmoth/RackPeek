using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Routers.Ports;

public class UpdateRouterPortUseCase(IResourceCollection repository) : IUseCase
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

        var Router = await repository.GetByNameAsync(name) as Router
                     ?? throw new NotFoundException($"Router '{name}' not found.");

        if (Router.Ports == null || index < 0 || index >= Router.Ports.Count)
            throw new NotFoundException($"Port index {index} not found on Router '{name}'.");

        var nic = Router.Ports[index];
        nic.Type = nicType;
        nic.Speed = speed;
        nic.Count = ports;

        await repository.UpdateAsync(Router);
    }
}