using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops;

public class DescribeLaptopUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<LaptopDescription> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var laptop = await repository.GetByNameAsync(name) as Laptop;
        if (laptop == null)
            throw new NotFoundException($"Laptop '{name}' not found.");

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