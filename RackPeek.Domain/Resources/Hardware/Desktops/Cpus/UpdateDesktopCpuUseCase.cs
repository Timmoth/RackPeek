using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Cpus;

public class UpdateDesktopCpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int index,
        string? model,
        int? cores,
        int? threads)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        if (desktop.Cpus == null || index < 0 || index >= desktop.Cpus.Count)
            throw new NotFoundException($"CPU index {index} not found on desktop '{name}'.");

        var cpu = desktop.Cpus[index];
        cpu.Model = model;
        cpu.Cores = cores;
        cpu.Threads = threads;

        await repository.UpdateAsync(desktop);
    }
}