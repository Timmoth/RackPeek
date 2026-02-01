using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace Tests.HardwareResources;

public class UpdateServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Updates_ram_ipmi_and_cpu_when_provided()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server
        {
            Name = "node01",
            Ipmi = false,
            Ram = new Ram { Size = 32 },
            Cpus = new List<Cpu>
            {
                new() { Model = "Old", Cores = 2, Threads = 4 }
            }
        });

        var sut = new UpdateServerUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            "node01",
            64,
            true
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Name == "node01" &&
            s.Ram.Size == 64 &&
            s.Ipmi == true
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_server_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns((Hardware?)null);

        var sut = new UpdateServerUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("node01", 64)
        );

        // Assert
        Assert.Equal("Server 'node01' not found.", ex.Message);
        await repo.DidNotReceive().UpdateAsync(Arg.Any<Server>());
    }

    [Fact]
    public async Task ExecuteAsync_Preserves_existing_values_when_not_provided()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server
        {
            Name = "node01",
            Ipmi = false,
            Ram = new Ram { Size = 32 },
            Cpus = new List<Cpu>
            {
                new() { Model = "Old", Cores = 2, Threads = 4 }
            }
        });

        var sut = new UpdateServerUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            "node01"
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Ram.Size == 32 &&
            s.Ipmi == false
        ));
    }
}