using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Reports;
public record DesktopHardwareReport(
    IReadOnlyList<DesktopHardwareRow> Desktops
);

public record DesktopHardwareRow(
    string Name,
    string CpuSummary,
    int TotalCores,
    int TotalThreads,
    int RamGb,
    int TotalStorageGb,
    int SsdStorageGb,
    int HddStorageGb,
    string NicSummary,
    string GpuSummary
);

public class DesktopHardwareReportUseCase(IHardwareRepository repository)
{
    public async Task<DesktopHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var desktops = hardware.OfType<Desktop>();

        var rows = desktops.Select(desktop =>
        {
            var totalCores = desktop.Cpus?.Sum(c => c.Cores) ?? 0;
            var totalThreads = desktop.Cpus?.Sum(c => c.Threads) ?? 0;

            var cpuSummary = desktop.Cpus == null
                ? "Unknown"
                : string.Join(", ",
                    desktop.Cpus
                        .GroupBy(c => c.Model)
                        .Select(g => $"{g.Count()}× {g.Key}"));

            var ramGb = desktop.Ram?.Size ?? 0;

            var totalStorage = desktop.Drives?.Sum(d => d.Size) ?? 0;
            var ssdStorage = desktop.Drives?
                .Where(d => d.Type == "ssd")
                .Sum(d => d.Size) ?? 0;
            var hddStorage = desktop.Drives?
                .Where(d => d.Type == "hdd")
                .Sum(d => d.Size) ?? 0;

            var nicSummary = desktop.Nics == null
                ? "Unknown"
                : string.Join(", ",
                    desktop.Nics
                        .GroupBy(n => n.Speed ?? 0)
                        .OrderBy(g => g.Key)
                        .Select(g =>
                        {
                            var count = g.Sum(n => n.Ports ?? 0);
                            return $"{count}×{g.Key}G";
                        }));

            var gpuSummary = desktop.Gpus == null
                ? "None"
                : string.Join(", ",
                    desktop.Gpus
                        .GroupBy(g => g.Model)
                        .Select(g => $"{g.Count()}× {g.Key}"));

            return new DesktopHardwareRow(
                Name: desktop.Name,
                CpuSummary: cpuSummary,
                TotalCores: totalCores,
                TotalThreads: totalThreads,
                RamGb: ramGb,
                TotalStorageGb: totalStorage,
                SsdStorageGb: ssdStorage,
                HddStorageGb: hddStorage,
                NicSummary: nicSummary,
                GpuSummary: gpuSummary
            );
        }).ToList();

        return new DesktopHardwareReport(rows);
    }
}