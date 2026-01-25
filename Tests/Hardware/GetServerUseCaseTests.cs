using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Crud;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.Hardware;

public class GetServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_server_when_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server { Name = "node01" });

        var sut = new GetServerUseCase(repo);

        // Act
        var server = await sut.ExecuteAsync("node01");

        // Assert
        Assert.NotNull(server);
        Assert.IsType<Server>(server);
        Assert.Equal("node01", server!.Name);
    }

    [Fact]
    public async Task ExecuteAsync_Returns_null_when_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new GetServerUseCase(repo);

        // Act
        var server = await sut.ExecuteAsync("node01");

        // Assert
        Assert.Null(server);
    }

    [Fact]
    public async Task ExecuteAsync_Returns_null_when_found_is_not_server()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Desktop { Name = "desktop1" });

        var sut = new GetServerUseCase(repo);

        // Act
        var server = await sut.ExecuteAsync("node01");

        // Assert
        Assert.Null(server);
    }
}