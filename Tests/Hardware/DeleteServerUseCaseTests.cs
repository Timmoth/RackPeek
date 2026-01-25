using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Server;

namespace Tests.Hardware;

public class DeleteServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Deletes_server_when_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server { Name = "node01" });

        var sut = new DeleteServerUseCase(repo);

        // Act
        await sut.ExecuteAsync("node01");

        // Assert
        await repo.Received(1).DeleteAsync("node01");
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_server_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new DeleteServerUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync("node01")
        );

        // Assert
        Assert.Equal("Server 'node01' not found.", ex.Message);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }
}