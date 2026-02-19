using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Gpus;

public class UpdateDesktopGpuUseCase(IResourceCollection repository) : IUseCase
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

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        if (desktop.Gpus == null || index < 0 || index >= desktop.Gpus.Count)
            throw new NotFoundException($"GPU index {index} not found on desktop '{name}'.");

        var gpu = desktop.Gpus[index];
        gpu.Model = model;
        gpu.Vram = vram;
        await repository.UpdateAsync(desktop);
    }
}