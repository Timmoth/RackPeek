using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Devices;

public record DeviceHardwareReport(
    IReadOnlyList<DeviceHardwareRow> Devices
);

public record DeviceHardwareRow(
    string Name,
    string Model
);

public class DeviceHardwareReportUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<DeviceHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var devices = hardware.OfType<Device>();

        var rows = devices.Select(d => new DeviceHardwareRow(
            d.Name,
            d.Model ?? "Unknown"
        )).ToList();

        return new DeviceHardwareReport(rows);
    }
}
