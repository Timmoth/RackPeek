using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Switches;

namespace Tests.HardwareResources.Switches;

public class DeleteSwitchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Deletes_switch_when_exists()
    {
        // Arrange
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("sw01").Returns(new Switch { Name = "sw01" });

        var sut = host.Get<DeleteSwitchUseCase>();

        // Act
        await sut.ExecuteAsync(
            "sw01"
        );

        // Assert
        await repo.Received(1).DeleteAsync("sw01");
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_switch_not_found()
    {
        // Arrange
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("sw01").Returns((Hardware?)null);

        var sut = host.Get<DeleteSwitchUseCase>();

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(async () =>
            await sut.ExecuteAsync(
                "sw01"
            )
        );

        // Assert
        Assert.Equal("Switch 'sw01' not found.", ex.Message);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }
}