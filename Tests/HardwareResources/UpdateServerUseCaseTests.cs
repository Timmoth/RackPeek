using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.Hardware.Servers.Ram;
using RackPeek.Domain.Resources.Models;

namespace Tests.HardwareResources;

public class UpdateServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Updates_ipmi_when_provided()
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
            true
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Name == "node01" &&
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
            sut.ExecuteAsync("node01", true)
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

    [Fact]
    public async Task SetServerRamUseCase_Updates_ram_when_provided()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server
        {
            Name = "node01",
            Ram = new Ram { Size = 32 }
        });

        var sut = new SetServerRamUseCase(repo);

        // Act
        await sut.ExecuteAsync("node01", 64, 3200);

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Ram.Size == 64 &&
            s.Ram.Mts == 3200
        ));
    }
}