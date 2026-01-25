namespace RackPeek.Domain.Resources.Hardware.Server;

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

public class DescribeServerUseCase(IHardwareRepository repository)
{
    public async Task<ServerDescription?> ExecuteAsync(string name)
    {
        var server = await repository.GetByNameAsync(name) as Models.Server;
        if (server == null)
            return null;

        var cpuSummary = server.Cpus == null
            ? "Unknown"
            : string.Join(", ",
                server.Cpus
                    .GroupBy(c => c.Model)
                    .Select(g => $"{g.Count()}× {g.Key}"));

        return new ServerDescription(
            Name: server.Name,
            CpuSummary: cpuSummary,
            TotalCores: server.Cpus?.Sum(c => c.Cores) ?? 0,
            TotalThreads: server.Cpus?.Sum(c => c.Threads) ?? 0,
            RamGb: server.Ram?.Size ?? 0,
            TotalStorageGb: server.Drives?.Sum(d => d.Size) ?? 0,
            NicPorts: server.Nics?.Sum(n => n.Ports) ?? 0,
            Ipmi: server.Ipmi ?? false
        );
    }
}