using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Connections;

public interface IGetConnectionForPortUseCase
{
    Task<Connection?> ExecuteAsync(PortReference port);
}

public class GetConnectionForPortUseCase(IResourceCollection repository)
    : IGetConnectionForPortUseCase
{
    public async Task<Connection?> ExecuteAsync(PortReference port)
    {
        port.Resource = Normalize.HardwareName(port.Resource);

        ThrowIfInvalid.ResourceName(port.Resource);

        return await repository.GetConnectionForPortAsync(port);
    }
}