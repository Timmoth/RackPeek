using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops.Drives;

public class UpdateDesktopDriveUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index, string? type, int? size)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop
                      ?? throw new NotFoundException($"Desktop '{name}' not found.");

        if (desktop.Drives == null || index < 0 || index >= desktop.Drives.Count)
            throw new NotFoundException($"Drive index {index} not found on desktop '{name}'.");

        var drive = desktop.Drives[index];
        drive.Type = type;
        drive.Size = size;

        await repository.UpdateAsync(desktop);
    }
}