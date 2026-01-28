using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Nics;

public class AddDesktopNicUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string desktopName, Nic nic)
    {
        var desktop = await repository.GetByNameAsync(desktopName) as Desktop
                      ?? throw new InvalidOperationException($"Desktop '{desktopName}' not found.");

        desktop.Nics ??= new List<Nic>();
        desktop.Nics.Add(nic);

        await repository.UpdateAsync(desktop);
    }
}