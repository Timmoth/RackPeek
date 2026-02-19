using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Gpus;

public class UpdateGpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int index,
        string? model,
        int? vram)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server)
            return;

        server.Gpus ??= [];

        if (index < 0 || index >= server.Gpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "GPU index out of range.");

        var gpu = server.Gpus[index];
        gpu.Model = model;
        gpu.Vram = vram;

        await repository.UpdateAsync(server);
    }
}