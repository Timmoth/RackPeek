using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class AddDesktopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new InvalidOperationException($"Desktop '{name}' already exists.");

        var desktop = new Desktop
        {
            Name = name,
            Cpus = new List<Cpu>(),
            Drives = new List<Drive>(),
            Nics = new List<Nic>(),
            Gpus = new List<Gpu>(),
            Ram = null
        };

        await repository.AddAsync(desktop);
    }
}