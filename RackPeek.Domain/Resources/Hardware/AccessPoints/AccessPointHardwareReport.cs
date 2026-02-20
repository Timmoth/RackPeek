using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public record AccessPointHardwareReport(
    IReadOnlyList<AccessPointHardwareRow> AccessPoints
);

public record AccessPointHardwareRow(
    string Name,
    string Model,
    double SpeedGb
);

public class AccessPointHardwareReportUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<AccessPointHardwareReport> ExecuteAsync()
    {
        var aps = await repository.GetAllOfTypeAsync<AccessPoint>();
        var rows = aps.Select(ap => new AccessPointHardwareRow(
            ap.Name,
            ap.Model ?? "Unknown",
            ap.Speed ?? 0
        )).ToList();

        return new AccessPointHardwareReport(rows);
    }
}