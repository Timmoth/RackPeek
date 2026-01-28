using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace Tests.HardwareResources;

public class ServerHardwareReportTests
{
    [Fact]
    public async Task Server_report_calculates_storage_and_cpu_correctly()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetAllAsync().Returns([
            new Server
            {
                Name = "srv1",
                Ipmi = true,
                Cpus = new List<Cpu>
                {
                    new() { Model = "Xeon", Cores = 4, Threads = 8 }
                },
                Ram = new Ram { Size = 32 },
                Drives = new List<Drive>
                {
                    new() { Type = "ssd", Size = 256 },
                    new() { Type = "hdd", Size = 2048 }
                },
                Nics = new List<Nic>
                {
                    new() { Speed = 10, Ports = 2 }
                },
                Gpus = new List<Gpu>
                {
                    new() { Model = "NVIDIA Tesla T4", Vram = 16 },
                    new() { Model = "NVIDIA Tesla T4", Vram = 16 },
                    new() { Model = "RTX 3080", Vram = 10 }
                }
            }
        ]);

        var sut = new ServerHardwareReportUseCase(repo);

        var report = await sut.ExecuteAsync();
        var server = report.Servers.Single();

        Assert.Equal(2304, server.TotalStorageGb);
        Assert.Equal(4, server.TotalCores);
        Assert.Equal(10, server.MaxNicSpeedGb);
        Assert.Equal(3, server.GpuCount);
        Assert.Equal(42, server.TotalGpuVramGb);
        Assert.Equal("2× NVIDIA Tesla T4, 1× RTX 3080", server.GpuSummary);
    }
}