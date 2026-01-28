using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public record AccessPointHardwareReport(
    IReadOnlyList<AccessPointHardwareRow> AccessPoints
);

public record AccessPointHardwareRow(
    string Name,
    string Model,
    double SpeedGb
);

public class AccessPointHardwareReportUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<AccessPointHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var aps = hardware.OfType<AccessPoint>();

        var rows = aps.Select(ap =>
        {
            return new AccessPointHardwareRow(
                ap.Name,
                ap.Model ?? "Unknown",
                ap.Speed ?? 0
            );
        }).ToList();

        return new AccessPointHardwareReport(rows);
    }
}