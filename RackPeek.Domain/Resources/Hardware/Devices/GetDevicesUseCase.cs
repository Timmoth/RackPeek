using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Devices;

public class GetDevicesUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<IReadOnlyList<Device>> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        return hardware.OfType<Device>().ToList();
    }
}
