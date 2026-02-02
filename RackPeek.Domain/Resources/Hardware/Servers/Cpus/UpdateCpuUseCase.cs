using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Cpus;

public class UpdateCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string serverName,
        int index,
        string model,
        int cores,
        int threads)
    {
        ThrowIfInvalid.ResourceName(serverName);

        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Server server) return;

        server.Cpus ??= [];

        if (index < 0 || index >= server.Cpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "CPU index out of range.");

        var cpu = server.Cpus[index];
        cpu.Model = model;
        cpu.Cores = cores;
        cpu.Threads = threads;

        await repository.UpdateAsync(server);
    }
}