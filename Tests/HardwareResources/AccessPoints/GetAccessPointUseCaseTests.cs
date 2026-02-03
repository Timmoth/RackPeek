using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.AccessPoints;

public class GetAccessPointUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_ap_when_it_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        var ap = new AccessPoint { Name = "ap01" };
        repo.GetByNameAsync("ap01").Returns(ap);

        var sut = new GetAccessPointUseCase(repo);

        // Act
        var result = await sut.ExecuteAsync("ap01");

        // Assert
        Assert.NotNull(result);
        Assert.Same(ap, result);
    }

    [Fact]
    public async Task ExecuteAsync_Returns_null_when_hardware_is_not_ap()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server { Name = "node01" });

        var sut = new GetAccessPointUseCase(repo);

        // Act
        await Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync("node01"));

    }
}