using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Switches;

namespace Tests.HardwareResources.Switches;

public class GetSwitchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_switch_when_it_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        var sw = new Switch { Name = "sw01" };
        repo.GetByNameAsync("sw01").Returns(sw);

        var sut = new GetSwitchUseCase(repo);

        // Act
        var result = await sut.ExecuteAsync("sw01");

        // Assert
        Assert.NotNull(result);
        Assert.Same(sw, result);
    }

    [Fact]
    public async Task ExecuteAsync_Returns_null_when_hardware_is_not_switch()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server { Name = "node01" });

        var sut = new GetSwitchUseCase(repo);

        // Act
        await Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync("node01"));

    }
}