using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Cpus;

public class RemoveCpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Server server) return;

        server.Cpus ??= [];

        if (index < 0 || index >= server.Cpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "CPU index out of range.");

        server.Cpus.RemoveAt(index);

        await repository.UpdateAsync(server);
    }
}