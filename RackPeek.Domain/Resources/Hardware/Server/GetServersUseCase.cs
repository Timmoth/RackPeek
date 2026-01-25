namespace RackPeek.Domain.Resources.Hardware.Server;

public class GetServersUseCase(IHardwareRepository repository)
{
    public async Task<IReadOnlyList<Models.Server>> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        return hardware.OfType<Models.Server>().ToList();
    }
}