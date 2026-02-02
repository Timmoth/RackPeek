using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops;

public class LaptopHardwareReportUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<LaptopHardwareReport> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        var laptops = hardware.OfType<Laptop>();

        var rows = laptops.Select(laptop =>
        {
            var totalCores = laptop.Cpus?.Sum(c => c.Cores) ?? 0;
            var totalThreads = laptop.Cpus?.Sum(c => c.Threads) ?? 0;

            var cpuSummary = laptop.Cpus == null
                ? "Unknown"
                : string.Join(", ",
                    laptop.Cpus
                        .GroupBy(c => c.Model)
                        .Select(g => $"{g.Count()}× {g.Key}"));

            var ramGb = laptop.Ram?.Size ?? 0;

            var totalStorage = laptop.Drives?.Sum(d => d.Size) ?? 0;
            var ssdStorage = laptop.Drives?
                .Where(d => d.Type == "ssd")
                .Sum(d => d.Size) ?? 0;
            var hddStorage = laptop.Drives?
                .Where(d => d.Type == "hdd")
                .Sum(d => d.Size) ?? 0;

            var gpuSummary = laptop.Gpus == null
                ? "None"
                : string.Join(", ",
                    laptop.Gpus
                        .GroupBy(g => g.Model)
                        .Select(g => $"{g.Count()}× {g.Key}"));

            return new LaptopHardwareRow(
                laptop.Name,
                cpuSummary,
                totalCores,
                totalThreads,
                ramGb,
                totalStorage,
                ssdStorage,
                hddStorage,
                gpuSummary
            );
        }).ToList();

        return new LaptopHardwareReport(rows);
    }
}

public record LaptopHardwareReport(
    IReadOnlyList<LaptopHardwareRow> Laptops
);

public record LaptopHardwareRow(
    string Name,
    string CpuSummary,
    int TotalCores,
    int TotalThreads,
    int RamGb,
    int TotalStorageGb,
    int SsdStorageGb,
    int HddStorageGb,
    string GpuSummary
);