using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class GetDesktopsUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<IReadOnlyList<Desktop>> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        return hardware.OfType<Desktop>().ToList();
    }
}