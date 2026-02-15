using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Devices;

public class GetDeviceUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Device> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Device device) throw new NotFoundException($"Device '{name}' not found.");

        return device;
    }
}
