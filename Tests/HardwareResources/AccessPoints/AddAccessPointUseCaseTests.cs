using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.AccessPoints;

public class AddAccessPointUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_new_ap_when_not_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ap01").Returns((Hardware?)null);

        var sut = new AddAccessPointUseCase(repo);

        // Act
        await sut.ExecuteAsync("ap01");

        // Assert
        await repo.Received(1).AddAsync(Arg.Is<AccessPoint>(ap =>
            ap.Name == "ap01"
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_ap_already_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ap01").Returns(new AccessPoint { Name = "ap01" });

        var sut = new AddAccessPointUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.ExecuteAsync("ap01")
        );

        // Assert
        await repo.DidNotReceive().AddAsync(Arg.Any<AccessPoint>());
    }
}