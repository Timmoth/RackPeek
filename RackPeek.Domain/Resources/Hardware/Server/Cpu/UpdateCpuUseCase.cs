namespace RackPeek.Domain.Resources.Hardware.Server.Cpu;

public class UpdateCpuUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(
        string serverName,
        int index,
        string model,
        int cores,
        int threads)
    {
        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Models.Server server)
        {
            return;
        }
        
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