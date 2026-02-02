using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Routers;

public class UpdateRouterUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        bool? managed = null,
        bool? poe = null
    )
    {
        ThrowIfInvalid.ResourceName(name);

        var routerResource = await repository.GetByNameAsync(name) as Router;
        if (routerResource == null)
            throw new NotFoundException($"Router '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            routerResource.Model = model;

        if (managed.HasValue)
            routerResource.Managed = managed.Value;

        if (poe.HasValue)
            routerResource.Poe = poe.Value;

        await repository.UpdateAsync(routerResource);
    }
}