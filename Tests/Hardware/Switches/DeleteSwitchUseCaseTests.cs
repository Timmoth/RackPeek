using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Switches;

namespace Tests.Hardware.Switches;

public class DeleteSwitchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Deletes_switch_when_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns(new Switch { Name = "sw01" });

        var sut = new DeleteSwitchUseCase(repo);

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
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new DeleteSwitchUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.ExecuteAsync(
                "sw01"
            )
        );

        // Assert
        Assert.Equal("Switch 'sw01' not found.", ex.Message);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }
}