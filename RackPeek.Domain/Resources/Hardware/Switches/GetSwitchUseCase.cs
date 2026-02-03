using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public class GetSwitchUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Switch> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Switch _switch)
        {
            throw new NotFoundException($"Switch '{name}' not found.");
        }
        
        return _switch;
    }
}