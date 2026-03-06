using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Connections;

public interface IAddConnectionUseCase
{
    Task ExecuteAsync(
        PortReference a,
        PortReference b,
        string? label = null,
        string? notes = null);
}

public class AddConnectionUseCase(IResourceCollection repository)
    : IAddConnectionUseCase
{
    public async Task ExecuteAsync(
        PortReference a,
        PortReference b,
        string? label,
        string? notes)
    {
        a.Resource = Normalize.HardwareName(a.Resource);
        b.Resource = Normalize.HardwareName(b.Resource);

        ThrowIfInvalid.ResourceName(a.Resource);
        ThrowIfInvalid.ResourceName(b.Resource);

        if (PortsMatch(a, b))
            throw new InvalidOperationException(
                "Cannot connect a port to itself.");

        await ValidatePortReference(a);
        await ValidatePortReference(b);

        // Overwrite behavior:
        // each PortReference may appear in only one connection,
        // so remove any existing connection involving either endpoint.
        await repository.RemoveConnectionsForPortAsync(a);
        await repository.RemoveConnectionsForPortAsync(b);

        var connection = new Connection
        {
            A = a,
            B = b,
            Label = label,
            Notes = notes
        };

        await repository.AddConnectionAsync(connection);
    }

    private async Task ValidatePortReference(PortReference port)
    {
        Resource resource =
            await repository.GetByNameAsync<Resource>(port.Resource)
            ?? throw new NotFoundException($"Resource '{port.Resource}' not found.");

        if (resource is not IPortResource pr || pr.Ports == null)
            throw new InvalidOperationException($"Resource '{port.Resource}' has no ports.");

        if (port.PortGroup < 0 || port.PortGroup >= pr.Ports.Count)
            throw new NotFoundException($"Port group {port.PortGroup} not found.");

        Port group = pr.Ports[port.PortGroup];

        if (port.PortIndex < 0 || port.PortIndex >= (group.Count ?? 0))
            throw new NotFoundException($"Port index {port.PortIndex} not found.");
    }

    private static bool PortsMatch(PortReference a, PortReference b)
    {
        return a.Resource.Equals(b.Resource, StringComparison.OrdinalIgnoreCase)
               && a.PortGroup == b.PortGroup
               && a.PortIndex == b.PortIndex;
    }
}
