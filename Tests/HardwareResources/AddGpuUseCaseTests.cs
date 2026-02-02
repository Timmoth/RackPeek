using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Servers.Gpus;

namespace Tests.HardwareResources;

public class AddGpuUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_gpu_when_server_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        var server = new Server
        {
            Name = "node01",
            Gpus = new List<Gpu>()
        };

        repo.GetByNameAsync("node01").Returns(server);

        var sut = new AddGpuUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            "node01",
            "RTX 4090",
            24
        );

        // Assert
        Assert.Single(server.Gpus);
        Assert.Equal("RTX 4090", server.Gpus[0].Model);
        Assert.Equal(24, server.Gpus[0].Vram);

        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Name == "node01" &&
            s.Gpus.Count == 1 &&
            s.Gpus[0].Model == "RTX 4090" &&
            s.Gpus[0].Vram == 24
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Initializes_gpu_list_when_null()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        var server = new Server
        {
            Name = "node01",
            Gpus = null
        };

        repo.GetByNameAsync("node01").Returns(server);

        var sut = new AddGpuUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            "node01",
            "RTX 3080",
            10
        );

        // Assert
        Assert.NotNull(server.Gpus);
        Assert.Single(server.Gpus);

        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Gpus != null &&
            s.Gpus.Count == 1 &&
            s.Gpus[0].Model == "RTX 3080"
        ));
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_server_does_not_exist()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01")
            .Returns((Hardware?)null);

        var sut = new AddGpuUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(async () =>
            await sut.ExecuteAsync(
                "node01",
                "RTX 4090",
                24
            )
        );

        // Assert
        await repo.DidNotReceive().AddAsync(Arg.Any<Server>());
    }
}