using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.Routers;

public class UpdateRouterUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        bool? managed = null,
        bool? poe = null,
        string? notes = null
    )
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
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
        if (notes != null)
        {
            routerResource.Notes = notes;
        }
        await repository.UpdateAsync(routerResource);
    }
}