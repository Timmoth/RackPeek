using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Reports;
public record AccessPointHardwareReport(
    IReadOnlyList<AccessPointHardwareRow> AccessPoints
);

public record AccessPointHardwareRow(
    string Name,
    string Model,
    int SpeedGb
);

public class AccessPointHardwareReportUseCase(IHardwareRepository repository)
{
    public async Task<AccessPointHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var aps = hardware.OfType<AccessPoint>();

        var rows = aps.Select(ap =>
        {
            return new AccessPointHardwareRow(
                Name: ap.Name,
                Model: ap.Model ?? "Unknown",
                SpeedGb: ap.Speed ?? 0
            );
        }).ToList();

        return new AccessPointHardwareReport(rows);
    }
}