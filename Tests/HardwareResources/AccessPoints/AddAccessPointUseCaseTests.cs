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
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("ap01").Returns((Hardware?)null);

        var sut = host.Get<AddAccessPointUseCase>();

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
        var host = new UsecaseTestHost();
        host.ResourceRepo.GetResourceKindAsync("ap01").Returns("Server");

        var sut = host.Get<AddAccessPointUseCase>();

        // Act
        var ex = await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.ExecuteAsync("ap01")
        );

        // Assert
        await host.HardwareRepo.DidNotReceive().AddAsync(Arg.Any<AccessPoint>());
    }
}