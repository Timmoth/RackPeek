using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.Servers.Gpus;

public class RemoveGpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server)
            return;

        server.Gpus ??= [];

        if (index < 0 || index >= server.Gpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "GPU index out of range.");

        server.Gpus.RemoveAt(index);

        await repository.UpdateAsync(server);
    }
}