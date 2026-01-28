using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public record UpsHardwareReport(
    IReadOnlyList<UpsHardwareRow> UpsUnits
);

public record UpsHardwareRow(
    string Name,
    string Model,
    int Va
);

public class UpsHardwareReportUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<UpsHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var upsUnits = hardware.OfType<Ups>();

        var rows = upsUnits.Select(ups =>
        {
            return new UpsHardwareRow(
                ups.Name,
                ups.Model ?? "Unknown",
                ups.Va ?? 0
            );
        }).ToList();

        return new UpsHardwareReport(rows);
    }
}