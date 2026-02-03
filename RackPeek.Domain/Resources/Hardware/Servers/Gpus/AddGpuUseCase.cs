using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Gpus;

public class AddGpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model,
        int? vram)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs
        
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server)
            throw new NotFoundException($"Server '{name}' not found.");

        server.Gpus ??= [];

        server.Gpus.Add(new Gpu
        {
            Model = model,
            Vram = vram
        });

        await repository.UpdateAsync(server);
    }
}