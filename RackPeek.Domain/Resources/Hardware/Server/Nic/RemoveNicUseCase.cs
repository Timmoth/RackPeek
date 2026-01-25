namespace RackPeek.Domain.Resources.Hardware.Server.Nic;

public class RemoveNicUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(string serverName, int index)
    {
        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Models.Server server)
            return;

        server.Nics ??= [];

        if (index < 0 || index >= server.Nics.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "NIC index out of range.");

        server.Nics.RemoveAt(index);

        await repository.UpdateAsync(server);
    }
}