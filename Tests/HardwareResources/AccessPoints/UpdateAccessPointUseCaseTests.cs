using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.AccessPoints;

public class UpdateAccessPointUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Throws_when_ap_not_found()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ap01").Returns((Hardware?)null);

        var sut = new UpdateAccessPointUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(async () =>
            await sut.ExecuteAsync("ap01")
        );

        // Assert
        Assert.Equal("Access point 'ap01' not found.", ex.Message);
        await repo.DidNotReceive().UpdateAsync(Arg.Any<AccessPoint>());
    }

    [Fact]
    public async Task ExecuteAsync_Updates_only_provided_fields()
    {
        // Arrange
        var existing = new AccessPoint
        {
            Name = "ap01",
            Model = "OldModel",
            Speed = 1.0
        };

        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ap01").Returns(existing);

        var sut = new UpdateAccessPointUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            "ap01",
            "NewModel",
            2.5
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<AccessPoint>(ap =>
            ap.Name == "ap01" &&
            ap.Model == "NewModel" &&
            ap.Speed == 2.5
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Does_not_update_model_when_empty_or_whitespace()
    {
        // Arrange
        var existing = new AccessPoint
        {
            Name = "ap01",
            Model = "KeepMe",
            Speed = 1.0
        };

        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ap01").Returns(existing);

        var sut = new UpdateAccessPointUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            "ap01",
            "   "
        );

        // Assert
        await repo.Received(1).UpdateAsync(Arg.Is<AccessPoint>(ap =>
            ap.Model == "KeepMe"
        ));
    }
}