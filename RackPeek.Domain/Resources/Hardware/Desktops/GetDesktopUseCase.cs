using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class GetDesktopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Desktop?> ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        return hardware as Desktop;
    }
}