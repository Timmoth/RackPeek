using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Cpus;

public class AddDesktopCpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, string? model, int? cores, int? threads)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        // ToDo validate / normalize all inputs

        var cpu = new Cpu
        {
            Model = model,
            Cores = cores,
            Threads = threads
        };

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{name}' not found.");

        desktop.Cpus ??= new List<Cpu>();
        desktop.Cpus.Add(cpu);

        await repository.UpdateAsync(desktop);
    }
}