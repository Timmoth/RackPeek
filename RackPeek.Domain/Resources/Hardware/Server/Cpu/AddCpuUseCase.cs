namespace RackPeek.Domain.Resources.Hardware.Server.Cpu;

public class AddCpuUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(
        string serverName,
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
        
        server.Cpus.Add(new Models.Cpu
        {
            Model = model,
            Cores = cores,
            Threads = threads
        });

        await repository.UpdateAsync(server);
    }
}