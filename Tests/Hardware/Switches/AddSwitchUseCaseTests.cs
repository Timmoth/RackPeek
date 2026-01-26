using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Switches;

namespace Tests.Hardware.Switches;

public class AddSwitchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_new_switch_when_not_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new AddSwitchUseCase(repo);

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
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns(new Switch { Name = "sw01" });

        var sut = new AddSwitchUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.ExecuteAsync(
                "sw01"
            )
        );

        // Assert
        Assert.Equal("Switch 'sw01' already exists.", ex.Message);
        await repo.DidNotReceive().AddAsync(Arg.Any<Switch>());
    }
}