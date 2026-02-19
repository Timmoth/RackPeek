using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Desktops;

public record DesktopDescription(
    string Name,
    string? Model,
    int CpuCount,
    string? RamSummary,
    int DriveCount,
    int NicCount,
    int GpuCount
);

public class DescribeDesktopUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<DesktopDescription> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var desktop = await repository.GetByNameAsync(name) as Desktop;
        if (desktop == null)
            throw new NotFoundException($"Desktop '{name}' not found.");

        var ramSummary = desktop.Ram == null
            ? "None"
            : $"{desktop.Ram.Size} GB @ {desktop.Ram.Mts} MT/s";

        return new DesktopDescription(
            desktop.Name,
            desktop.Model,
            desktop.Cpus?.Count ?? 0,
            ramSummary,
            desktop.Drives?.Count ?? 0,
            desktop.Nics?.Count ?? 0,
            desktop.Gpus?.Count ?? 0
        );
    }
}