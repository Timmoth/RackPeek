using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Switches;

namespace Tests.Hardware.Switches;

public class UpdateSwitchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Throws_when_switch_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new UpdateSwitchUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.ExecuteAsync("sw01")
        );

        // Assert
        Assert.Equal("Switch 'sw01' not found.", ex.Message);
        await repo.DidNotReceive().UpdateAsync(Arg.Any<Switch>());
    }

    [Fact]
    public async Task ExecuteAsync_Updates_only_provided_fields()
    {
        // Arrange
        var existing = new Switch
        {
            Name = "sw01",
            Model = "OldModel",
            Managed = false,
            Poe = false
        };

        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns(existing);

        var sut = new UpdateSwitchUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            "sw01",
            "NewModel",
            poe: true
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<Switch>(s =>
                s.Name == "sw01" &&
                s.Model == "NewModel" && // updated
                s.Managed == false && // unchanged
                s.Poe == true // updated
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Does_not_update_model_when_empty_or_whitespace()
    {
        // Arrange
        var existing = new Switch
        {
            Name = "sw01",
            Model = "KeepMe",
            Managed = true,
            Poe = true
        };

        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns(existing);

        var sut = new UpdateSwitchUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            "sw01",
            "   "
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<Switch>(s =>
            s.Model == "KeepMe"
        ));
    }
}