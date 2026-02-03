using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace Tests.HardwareResources;

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
            Cpus = new List<Cpu>
            {
                new() { Model = "Xeon", Cores = 4, Threads = 8 },
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
        repo.GetByNameAsync("node01").Returns((Hardware?)null);

        var sut = new DescribeServerUseCase(repo);

        // Act
        await Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync("node01"));

    }
}