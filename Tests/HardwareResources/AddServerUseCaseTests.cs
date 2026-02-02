using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Servers;

namespace Tests.HardwareResources;

public class AddServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_new_server_when_not_exists()
    {
        // Arrange
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("node01").Returns((Hardware?)null);

        var sut = host.Get<AddServerUseCase>();

        // Act
        await sut.ExecuteAsync(
            "node01"
        );

        // Assert
        await repo.Received(1).AddAsync(Arg.Is<Server>(s =>
            s.Name == "node01"
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_server_already_exists()
    {
        // Arrange
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        host.ResourceRepo.GetResourceKindAsync("node01").Returns("Server");

        var sut = host.Get<AddServerUseCase>();
        
        // Act
        var ex = await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.ExecuteAsync(
                "node01"
            )
        );

        // Assert
        await repo.DidNotReceive().AddAsync(Arg.Any<Server>());
    }
}