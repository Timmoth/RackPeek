using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public record SwitchHardwareReport(
    IReadOnlyList<SwitchHardwareRow> Switches
);

public record SwitchHardwareRow(
    string Name,
    string Model,
    bool Managed,
    bool Poe,
    int TotalPorts,
    double MaxPortSpeedGb,
    string PortSummary
);

public class SwitchHardwareReportUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<SwitchHardwareReport> ExecuteAsync()
    {
        var switches = await repository.GetAllOfTypeAsync<Switch>();

        var rows = switches.Select(sw =>
        {
            var totalPorts = sw.Ports?.Sum(p => p.Count ?? 0) ?? 0;

            var maxSpeed = sw.Ports?
                .Max(p => p.Speed ?? 0) ?? 0;

            var portSummary = sw.Ports == null
                ? "Unknown"
                : string.Join(", ",
                    sw.Ports
                        .GroupBy(p => p.Speed ?? 0)
                        .OrderBy(g => g.Key)
                        .Select(g =>
                        {
                            var count = g.Sum(p => p.Count ?? 0);
                            return $"{count}×{g.Key}G";
                        }));

            return new SwitchHardwareRow(
                sw.Name,
                sw.Model ?? "Unknown",
                sw.Managed ?? false,
                sw.Poe ?? false,
                totalPorts,
                maxSpeed,
                portSummary
            );
        }).ToList();

        return new SwitchHardwareReport(rows);
    }
}