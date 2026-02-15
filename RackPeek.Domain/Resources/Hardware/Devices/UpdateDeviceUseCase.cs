using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Devices;

public class UpdateDeviceUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        string? notes = null
    )
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var device = await repository.GetByNameAsync(name) as Device;
        if (device == null)
            throw new NotFoundException($"Device '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            device.Model = model;

        if (notes != null)
            device.Notes = notes;

        await repository.UpdateAsync(device);
    }
}
