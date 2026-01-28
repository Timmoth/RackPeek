using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Cpus;

public class AddCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string serverName,
        string model,
        int cores,
        int threads)
    {
        var hardware = await repository.GetByNameAsync(serverName);

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