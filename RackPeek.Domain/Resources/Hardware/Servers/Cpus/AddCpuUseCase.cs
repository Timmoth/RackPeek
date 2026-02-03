using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Cpus;

public class AddCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string model,
        int cores,
        int threads)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server) return;

        server.Cpus ??= [];

        server.Cpus.Add(new Cpu
        {
            Model = model,
            Cores = cores,
            Threads = threads
        });

        await repository.UpdateAsync(server);
    }
}