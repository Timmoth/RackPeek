using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Server;

namespace Tests.Hardware;

public class DescribeServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_description_when_server_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server
        {
            Name = "node01",
            Ipmi = true,
            Cpus = new()
            {
                new Cpu { Model = "Xeon", Cores = 4, Threads = 8 },
                new Cpu { Model = "Xeon", Cores = 4, Threads = 8 }
            },
            Ram = new Ram { Size = 32 },
            Drives = new()
            {
                new Drive { Type = "ssd", Size = 256 },
                new Drive { Type = "hdd", Size = 2048 }
            },
            Nics = new()
            {
                new Nic { Speed = 10, Ports = 2 }
            }
        });

        var sut = new DescribeServerUseCase(repo);

        // Act
        var description = await sut.ExecuteAsync("node01");

        // Assert
        Assert.NotNull(description);
        Assert.Equal("node01", description!.Name);
        Assert.Equal("2× Xeon", description.CpuSummary);
        Assert.Equal(8, description.TotalCores);
        Assert.Equal(16, description.TotalThreads);
        Assert.Equal(32, description.RamGb);
        Assert.Equal(2304, description.TotalStorageGb);
        Assert.Equal(2, description.NicPorts);
        Assert.True(description.Ipmi);
    }

    [Fact]
    public async Task ExecuteAsync_Returns_null_when_server_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new DescribeServerUseCase(repo);

        // Act
        var description = await sut.ExecuteAsync("node01");

        // Assert
        Assert.Null(description);
    }
}
