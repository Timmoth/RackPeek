using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Connections;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.UseCases.Ports;

public interface IRemovePortUseCase<T> : IResourceUseCase<T>
    where T : Resource {
    Task ExecuteAsync(string name, int index);
}

public class RemovePortUseCase<T>(IResourceCollection repository)
    : IRemovePortUseCase<T> where T : Resource {
    public async Task ExecuteAsync(string name, int index) {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        T resource = await repository.GetByNameAsync<T>(name)
                     ?? throw new NotFoundException($"Resource '{name}' not found.");

        if (resource is not IPortResource pr)
            throw new NotFoundException($"Resource '{name}' not found.");

        if (pr.Ports == null || index < 0 || index >= pr.Ports.Count)
            throw new NotFoundException($"Port index {index} not found on '{name}'.");

        IReadOnlyList<Connection> connections =
            await repository.GetConnectionsForResourceAsync(name);

        var toRemove = new List<Connection>();
        var toAdd = new List<Connection>();

        foreach (Connection connection in connections) {
            var changed = false;

            PortReference a = connection.A;
            PortReference b = connection.B;

            // handle A side
            if (a.Resource.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                if (a.PortGroup == index) {
                    toRemove.Add(connection);
                    continue;
                }

                if (a.PortGroup > index) {
                    a = new PortReference {
                        Resource = a.Resource,
                        PortGroup = a.PortGroup - 1,
                        PortIndex = a.PortIndex
                    };

                    changed = true;
                }
            }

            // handle B side
            if (b.Resource.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                if (b.PortGroup == index) {
                    toRemove.Add(connection);
                    continue;
                }

                if (b.PortGroup > index) {
                    b = new PortReference {
                        Resource = b.Resource,
                        PortGroup = b.PortGroup - 1,
                        PortIndex = b.PortIndex
                    };

                    changed = true;
                }
            }

            if (changed) {
                toRemove.Add(connection);

                toAdd.Add(new Connection {
                    A = a,
                    B = b,
                    Label = connection.Label,
                    Notes = connection.Notes
                });
            }
        }

        foreach (Connection connection in toRemove)
            await repository.RemoveConnectionAsync(connection);

        foreach (Connection connection in toAdd)
            await repository.AddConnectionAsync(connection);

        pr.Ports.RemoveAt(index);

        await repository.UpdateAsync(resource);
    }
}
