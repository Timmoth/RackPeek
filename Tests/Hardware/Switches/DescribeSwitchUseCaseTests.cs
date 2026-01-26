using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Switches;

namespace Tests.Hardware.Switches;

public class DescribeSwitchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_null_when_switch_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new DescribeSwitchUseCase(repo);

        // Act
        var result = await sut.ExecuteAsync("sw01");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_Returns_defaults_when_switch_has_no_ports()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns(new Switch
        {
            Name = "sw01",
            Model = "TestModel",
            Managed = true,
            Poe = false,
            Ports = null
        });

        var sut = new DescribeSwitchUseCase(repo);

        // Act
        var result = await sut.ExecuteAsync("sw01");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("sw01", result.Name);
        Assert.Equal("TestModel", result.Model);
        Assert.True(result.Managed);
        Assert.False(result.Poe);
        Assert.Equal(0, result.TotalPorts);
        Assert.Equal(0, result.TotalSpeedGb);
        Assert.Equal(string.Empty, result.PortSummary);
    }

    [Fact]
    public async Task ExecuteAsync_Calculates_totals_and_summary_from_ports()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("sw01").Returns(new Switch
        {
            Name = "sw01",
            Model = "CoreSwitch",
            Managed = true,
            Poe = true,
            Ports = new List<Port>
            {
                new() { Type = "RJ45", Speed = 1, Count = 24 },
                new() { Type = "SFP+", Speed = 10, Count = 4 }
            }
        });

        var sut = new DescribeSwitchUseCase(repo);

        // Act
        var result = await sut.ExecuteAsync("sw01");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("sw01", result.Name);
        Assert.Equal("CoreSwitch", result.Model);
        Assert.True(result.Managed);
        Assert.True(result.Poe);

        Assert.Equal(28, result.TotalPorts); // 24 + 4
        Assert.Equal(64, result.TotalSpeedGb); // (24 * 1) + (4 * 10)

        Assert.Equal(
            "RJ45: 24 ports (24 Gb total), SFP+: 4 ports (40 Gb total)",
            result.PortSummary
        );
    }
}