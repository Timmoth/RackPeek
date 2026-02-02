using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Drives;

public class RemoveDriveUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string serverName, int index)
    {
        ThrowIfInvalid.ResourceName(serverName);

        var hardware = await repository.GetByNameAsync(serverName);
        if (hardware is not Server server) return;
        server.Drives ??= [];
        if (index < 0 || index >= server.Drives.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Drive index out of range.");
        server.Drives.RemoveAt(index);
        await repository.UpdateAsync(server);
    }
}