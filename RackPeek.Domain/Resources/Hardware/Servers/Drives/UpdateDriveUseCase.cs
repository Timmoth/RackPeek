using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Drives;

public class UpdateDriveUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index, string type, int size)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Server server) return;

        server.Drives ??= [];
        if (index < 0 || index >= server.Drives.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Drive index out of range.");
        var drive = server.Drives[index];
        drive.Type = type;
        drive.Size = size;
        await repository.UpdateAsync(server);
    }
}