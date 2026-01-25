using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Reports;
public record FirewallHardwareReport(
    IReadOnlyList<FirewallHardwareRow> Firewalls
);

public record FirewallHardwareRow(
    string Name,
    string Model,
    bool Managed,
    bool Poe,
    int TotalPorts,
    int MaxPortSpeedGb,
    string PortSummary
);
public class FirewallHardwareReportUseCase(IHardwareRepository repository)
{
    public async Task<FirewallHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var firewalls = hardware.OfType<Firewall>();

        var rows = firewalls.Select(sw =>
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

            return new FirewallHardwareRow(
                Name: sw.Name,
                Model: sw.Model ?? "Unknown",
                Managed: sw.Managed ?? false,
                Poe: sw.Poe ?? false,
                TotalPorts: totalPorts,
                MaxPortSpeedGb: maxSpeed,
                PortSummary: portSummary
            );
        }).ToList();

        return new FirewallHardwareReport(rows);
    }
}