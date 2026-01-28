using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Cpus;

public class RemoveCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string serverName,
        int index)
    {
        var hardware = await repository.GetByNameAsync(serverName);
        if (hardware is not Server server) return;

        server.Cpus ??= [];

        if (index < 0 || index >= server.Cpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "CPU index out of range.");

        server.Cpus.RemoveAt(index);

        await repository.UpdateAsync(server);
    }
}