using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class AddDesktopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new ConflictException($"Desktop '{name}' already exists.");

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