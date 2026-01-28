using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class GetServerUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Server?> ExecuteAsync(string name)
    {
        var hardware = await repository.GetByNameAsync(name);
        return hardware as Server;
    }
}