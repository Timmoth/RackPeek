using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public record UpsHardwareReport(
    IReadOnlyList<UpsHardwareRow> UpsUnits
);

public record UpsHardwareRow(
    string Name,
    string Model,
    int Va
);

public class UpsHardwareReportUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<UpsHardwareReport> ExecuteAsync()
    {
        var upsUnits = await repository.GetAllOfTypeAsync<Ups>();

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