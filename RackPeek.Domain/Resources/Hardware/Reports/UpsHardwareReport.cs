using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Reports;
public record UpsHardwareReport(
    IReadOnlyList<UpsHardwareRow> UpsUnits
);

public record UpsHardwareRow(
    string Name,
    string Model,
    int Va
);

public class UpsHardwareReportUseCase(IHardwareRepository repository)
{
    public async Task<UpsHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var upsUnits = hardware.OfType<Ups>();

        var rows = upsUnits.Select(ups =>
        {
            return new UpsHardwareRow(
                Name: ups.Name,
                Model: ups.Model ?? "Unknown",
                Va: ups.Va ?? 0
            );
        }).ToList();

        return new UpsHardwareReport(rows);
    }
}