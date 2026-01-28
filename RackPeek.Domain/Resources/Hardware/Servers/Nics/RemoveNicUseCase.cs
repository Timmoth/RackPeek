using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Nics;

public class RemoveNicUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string serverName, int index)
    {
        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Server server)
            return;

        server.Nics ??= [];

        if (index < 0 || index >= server.Nics.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "NIC index out of range.");

        server.Nics.RemoveAt(index);

        await repository.UpdateAsync(server);
    }
}