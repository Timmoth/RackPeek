using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Switches;

namespace Tests.Hardware.Switches;

public class GetSwitchesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_only_switches()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetAllAsync().Returns([
            new Switch { Name = "sw01" },
            new Server { Name = "node01" },
            new Switch { Name = "sw02" }
        ]);

        var sut = new GetSwitchesUseCase(repo);

        // Act
        var result = await sut.ExecuteAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.IsType<Switch>(s));
        Assert.Contains(result, s => s.Name == "sw01");
        Assert.Contains(result, s => s.Name == "sw02");
    }

    [Fact]
    public async Task ExecuteAsync_Returns_empty_list_when_no_switches_exist()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetAllAsync().Returns([
            new Server { Name = "node01" }
        ]);

        var sut = new GetSwitchesUseCase(repo);

        // Act
        var result = await sut.ExecuteAsync();

        // Assert
        Assert.Empty(result);
    }
}