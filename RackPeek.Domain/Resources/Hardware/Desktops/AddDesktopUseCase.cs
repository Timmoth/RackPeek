using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class AddDesktopUseCase(IHardwareRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var existingResourceKind = await resourceRepo.GetResourceKindAsync(name);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{name}' already exists.");

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