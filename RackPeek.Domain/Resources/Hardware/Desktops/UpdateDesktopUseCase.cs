using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class UpdateDesktopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null
    )
    {
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop;
        if (desktop == null)
            throw new NotFoundException($"Desktop '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            desktop.Model = model;

        await repository.UpdateAsync(desktop);
    }
}