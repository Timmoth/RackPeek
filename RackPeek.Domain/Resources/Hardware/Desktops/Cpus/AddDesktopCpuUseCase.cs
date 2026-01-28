using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Cpus;

public class AddDesktopCpuUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string desktopName, Cpu cpu)
    {
        var desktop = await repository.GetByNameAsync(desktopName) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{desktopName}' not found.");

        desktop.Cpus ??= new List<Cpu>();
        desktop.Cpus.Add(cpu);

        await repository.UpdateAsync(desktop);
    }
}