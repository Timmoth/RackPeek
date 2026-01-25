using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Crud;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.Hardware;

public class GetServersUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_only_servers()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetAllAsync().Returns(new List<RackPeek.Domain.Resources.Hardware.Models.Hardware>
        {
            new Server { Name = "server1" },
            new Desktop { Name = "desktop1" },
            new Server { Name = "server2" }
        });

        var sut = new GetServersUseCase(repo);

        // Act
        var servers = await sut.ExecuteAsync();

        // Assert
        Assert.Equal(2, servers.Count);
        Assert.All(servers, s => Assert.IsType<Server>(s));
        Assert.Contains(servers, s => s.Name == "server1");
        Assert.Contains(servers, s => s.Name == "server2");
    }

    [Fact]
    public async Task ExecuteAsync_Returns_empty_when_no_servers()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetAllAsync().Returns(new List<RackPeek.Domain.Resources.Hardware.Models.Hardware>
        {
            new Desktop { Name = "desktop1" }
        });

        var sut = new GetServersUseCase(repo);

        // Act
        var servers = await sut.ExecuteAsync();

        // Assert
        Assert.Empty(servers);
    }
}