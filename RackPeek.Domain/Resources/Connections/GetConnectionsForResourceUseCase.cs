using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Connections;

public interface IGetConnectionsForResourceUseCase
{
    Task<IReadOnlyList<Connection>> ExecuteAsync(string resource);
}

public class GetConnectionsForResourceUseCase(IResourceCollection repository)
    : IGetConnectionsForResourceUseCase
{
    public async Task<IReadOnlyList<Connection>> ExecuteAsync(string resource)
    {
        resource = Normalize.HardwareName(resource);

        ThrowIfInvalid.ResourceName(resource);

        return await repository.GetConnectionsForResourceAsync(resource);
    }
}