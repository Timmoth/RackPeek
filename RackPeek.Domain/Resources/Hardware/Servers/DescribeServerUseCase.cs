using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public record ServerDescription(
    string Name,
    string CpuSummary,
    int TotalCores,
    int TotalThreads,
    int RamGb,
    int TotalStorageGb,
    int NicPorts,
    bool Ipmi
);

public class DescribeServerUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<ServerDescription?> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var server = await repository.GetByNameAsync(name) as Server;
        if (server == null)
            return null;

        var cpuSummary = server.Cpus == null
            ? "Unknown"
            : string.Join(", ",
                server.Cpus
                    .GroupBy(c => c.Model)
                    .Select(g => $"{g.Count()}× {g.Key}"));

        return new ServerDescription(
            server.Name,
            cpuSummary,
            server.Cpus?.Sum(c => c.Cores) ?? 0,
            server.Cpus?.Sum(c => c.Threads) ?? 0,
            server.Ram?.Size ?? 0,
            server.Drives?.Sum(d => d.Size) ?? 0,
            server.Nics?.Sum(n => n.Ports) ?? 0,
            server.Ipmi ?? false
        );
    }
}