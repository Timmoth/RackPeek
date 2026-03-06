using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Connections;

public interface IRemoveConnectionUseCase
{
    Task ExecuteAsync(PortReference port);
}

public class RemoveConnectionUseCase(IResourceCollection repository)
    : IRemoveConnectionUseCase
{
    public async Task ExecuteAsync(PortReference port)
    {
        port.Resource = Normalize.HardwareName(port.Resource);

        ThrowIfInvalid.ResourceName(port.Resource);

        await repository.RemoveConnectionsForPortAsync(port);
    }
}
