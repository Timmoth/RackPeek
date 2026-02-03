using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Switches;

namespace Tests.HardwareResources.Switches;

public class AddSwitchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_new_switch_when_not_exists()
    {
        // Arrange
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("sw01").Returns((Hardware?)null);

        var sut = host.Get<AddSwitchUseCase>();

        // Act
        await sut.ExecuteAsync(
            "sw01"
        );

        // Assert
        await repo.Received(1).AddAsync(Arg.Is<Switch>(s =>
            s.Name == "sw01"
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_switch_already_exists()
    {
        // Arrange
        var host = new UsecaseTestHost();
        host.ResourceRepo.GetResourceKindAsync("sw01").Returns("Server");

        var sut = host.Get<AddSwitchUseCase>();

        // Act
        var ex = await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.ExecuteAsync(
                "sw01"
            )
        );

        // Assert
        await host.HardwareRepo.DidNotReceive().AddAsync(Arg.Any<Switch>());
    }
}