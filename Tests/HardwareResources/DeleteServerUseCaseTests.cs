using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace Tests.HardwareResources;

public class DeleteServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Deletes_server_when_exists()
    {
        // Arrange
        var host = new UsecaseTestHost();
        host.HardwareRepo.GetByNameAsync("node01").Returns(new Server { Name = "node01" });

        var sut = host.Get<DeleteServerUseCase>();

        // Act
        await sut.ExecuteAsync("node01");

        // Assert
        await host.HardwareRepo.Received(1).DeleteAsync("node01");
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_server_not_found()
    {
        // Arrange
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("node01").Returns((Hardware?)null);

        var sut = host.Get<DeleteServerUseCase>();

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("node01")
        );

        // Assert
        Assert.Equal("Server 'node01' not found.", ex.Message);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }
}