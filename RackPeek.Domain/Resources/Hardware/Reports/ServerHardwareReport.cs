using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Reports;

public record ServerHardwareReport(
    IReadOnlyList<ServerHardwareRow> Servers
);

public record ServerHardwareRow(
    string Name,
    string CpuSummary,
    int TotalCores,
    int TotalThreads,
    int RamGb,
    int TotalStorageGb,
    int SsdStorageGb,
    int HddStorageGb,
    int TotalNicPorts,
    int MaxNicSpeedGb,
    int GpuCount,
    int TotalGpuVramGb,
    string GpuSummary,
    bool Ipmi
    
);

public class ServerHardwareReportUseCase(IHardwareRepository repository)
{
    public async Task<ServerHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var servers = hardware.OfType<Models.Server>();

        var rows = servers.Select(server =>
        {
            var totalCores = server.Cpus?.Sum(c => c.Cores) ?? 0;
            var totalThreads = server.Cpus?.Sum(c => c.Threads) ?? 0;

            var cpuSummary = server.Cpus == null
                ? "Unknown"
                : string.Join(", ",
                    server.Cpus
                        .GroupBy(c => c.Model)
                        .Select(g => $"{g.Count()}× {g.Key}"));

            var ramGb = server.Ram?.Size ?? 0;

            var totalStorage = server.Drives?.Sum(d => d.Size) ?? 0;
            var ssdStorage = server.Drives?
                .Where(d => d.Type == "ssd")
                .Sum(d => d.Size) ?? 0;
            var hddStorage = server.Drives?
                .Where(d => d.Type == "hdd")
                .Sum(d => d.Size) ?? 0;

            var totalNicPorts = server.Nics?.Sum(n => n.Ports) ?? 0;
            var maxNicSpeed = server.Nics?.Max(n => n.Speed) ?? 0;
            
            var gpuCount = server.Gpus?.Count ?? 0;

            var totalGpuVram = server.Gpus?
                .Sum(g => g.Vram) ?? 0;

            var gpuSummary = server.Gpus == null || server.Gpus.Count == 0
                ? "None"
                : string.Join(", ",
                    server.Gpus
                        .GroupBy(g => g.Model)
                        .Select(g => $"{g.Count()}× {g.Key}"));


            return new ServerHardwareRow(
                Name: server.Name,
                CpuSummary: cpuSummary,
                TotalCores: totalCores,
                TotalThreads: totalThreads,
                RamGb: ramGb,
                TotalStorageGb: totalStorage,
                SsdStorageGb: ssdStorage,
                HddStorageGb: hddStorage,
                TotalNicPorts: totalNicPorts,
                MaxNicSpeedGb: maxNicSpeed,
                GpuCount: gpuCount,
                TotalGpuVramGb: totalGpuVram,
                GpuSummary: gpuSummary,
                Ipmi: server.Ipmi ?? false
            );
        }).ToList();

        return new ServerHardwareReport(rows);
    }
}
