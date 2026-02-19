using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Routers.Ports;

public class RemoveRouterPortUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var Router = await repository.GetByNameAsync(name) as Router
                     ?? throw new NotFoundException($"Router '{name}' not found.");

        if (Router.Ports == null || index < 0 || index >= Router.Ports.Count)
            throw new NotFoundException($"Port index {index} not found on Router '{name}'.");

        Router.Ports.RemoveAt(index);

        await repository.UpdateAsync(Router);
    }
}