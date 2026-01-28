using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Gpus;

public class AddGpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string serverName,
        string model,
        int vram)
    {
        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Server server)
            return;

        server.Gpus ??= [];

        server.Gpus.Add(new Gpu
        {
            Model = model,
            Vram = vram
        });

        await repository.UpdateAsync(server);
    }
}