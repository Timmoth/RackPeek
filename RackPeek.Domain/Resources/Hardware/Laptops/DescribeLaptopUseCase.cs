using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops;

public class DescribeLaptopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<LaptopDescription?> ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop;
        if (laptop == null)
            return null;

        var ramSummary = laptop.Ram == null
            ? "None"
            : $"{laptop.Ram.Size} GB @ {laptop.Ram.Mts} MT/s";

        return new LaptopDescription(
            laptop.Name,
            laptop.Cpus?.Count ?? 0,
            ramSummary,
            laptop.Drives?.Count ?? 0,
            laptop.Gpus?.Count ?? 0
        );
    }
}

public record LaptopDescription(
    string Name,
    int CpuCount,
    string? RamSummary,
    int DriveCount,
    int GpuCount
);