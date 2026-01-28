using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public class GetSwitchUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Switch?> ExecuteAsync(string name)
    {
        var hardware = await repository.GetByNameAsync(name);
        return hardware as Switch;
    }
}