namespace RackPeek.Domain.Resources.Hardware.Server.Nic;

public class AddNicUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(
        string serverName,
        string type,
        int speed,
        int ports)
    {
        var hardware = await repository.GetByNameAsync(serverName);

        if (hardware is not Models.Server server)
            return;

        server.Nics ??= [];

        server.Nics.Add(new Models.Nic
        {
            Type = type,
            Speed = speed,
            Ports = ports
        });

        await repository.UpdateAsync(server);
    }
}