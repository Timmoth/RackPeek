using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Crud;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.Hardware;

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
                new Cpu { Model = "Old", Cores = 2, Threads = 4 }
            }
        });

        var sut = new UpdateServerUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            name: "node01",
            ramGb: 64,
            ipmi: true,
            cpuModel: "Xeon E3",
            cores: 4,
            threads: 8
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Name == "node01" &&
            s.Ram.Size == 64 &&
            s.Ipmi == true &&
            s.Cpus != null &&
            s.Cpus.First().Model == "Xeon E3" &&
            s.Cpus.First().Cores == 4 &&
            s.Cpus.First().Threads == 8
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_server_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new UpdateServerUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync("node01", ramGb: 64)
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
                new Cpu { Model = "Old", Cores = 2, Threads = 4 }
            }
        });

        var sut = new UpdateServerUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            name: "node01",
            ramGb: null,
            ipmi: null,
            cpuModel: null,
            cores: null,
            threads: null
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Ram.Size == 32 &&
            s.Ipmi == false &&
            s.Cpus.First().Model == "Old" &&
            s.Cpus.First().Cores == 2 &&
            s.Cpus.First().Threads == 4
        ));
    }
}
