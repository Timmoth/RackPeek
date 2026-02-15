using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Devices;

public record DeviceDescription(
    string Name,
    string? Model
);

public class DescribeDeviceUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<DeviceDescription> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var device = await repository.GetByNameAsync(name) as Device;
        if (device == null)
            throw new NotFoundException($"Device '{name}' not found.");

        return new DeviceDescription(
            device.Name,
            device.Model
        );
    }
}
