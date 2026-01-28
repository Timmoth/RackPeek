using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class GetServersUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<IReadOnlyList<Server>> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        return hardware.OfType<Server>().ToList();
    }
}