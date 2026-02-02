using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Routers;

public class AddRouterUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        // basic guard rails
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new ConflictException($"Router '{name}' already exists.");

        var routerResource = new Router
        {
            Name = name
        };

        await repository.AddAsync(routerResource);
    }
}