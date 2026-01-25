namespace RackPeek.Domain.Resources.Hardware.Server.Cpu;

public class RemoveCpuUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(
        string serverName,
        int index)
    {
        var hardware = await repository.GetByNameAsync(serverName);
        if (hardware is not Models.Server server)
        {
            return;
        }
        
        server.Cpus ??= [];

        if (index < 0 || index >= server.Cpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "CPU index out of range.");

        server.Cpus.RemoveAt(index);

        await repository.UpdateAsync(server);
    }
}