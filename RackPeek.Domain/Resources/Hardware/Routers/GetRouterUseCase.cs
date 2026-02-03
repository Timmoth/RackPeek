using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Routers;

public class GetRouterUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Router> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Router router)
        {
            throw new NotFoundException($"Router '{name}' not found.");
        }
        
        return router;
    }
}