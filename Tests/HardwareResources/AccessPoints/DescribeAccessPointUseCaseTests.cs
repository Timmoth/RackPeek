using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.AccessPoints;

public class DescribeAccessPointUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_null_when_ap_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ap01").Returns((Hardware?)null);

        var sut = new DescribeAccessPointUseCase(repo);

        // Act
        await Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync("ap01"));
    }

    [Fact]
    public async Task ExecuteAsync_Returns_description_when_ap_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ap01").Returns(new AccessPoint
        {
            Name = "ap01",
            Model = "U6-Lite",
            Speed = 1.2
        });

        var sut = new DescribeAccessPointUseCase(repo);

        // Act
        var result = await sut.ExecuteAsync("ap01");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ap01", result.Name);
        Assert.Equal("U6-Lite", result.Model);
        Assert.Equal(1.2, result.Speed);
    }
}