using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.Desktops;

public class DeleteDesktopUseCaseTests
{
    [Fact]
    public async Task Deletes_Desktop()
    {
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("desk1").Returns(new Desktop { Name = "desk1" });

        var sut = host.Get<DeleteDesktopUseCase>();

        await sut.ExecuteAsync("desk1");

        await repo.Received().DeleteAsync("desk1");
    }

    [Fact]
    public async Task Throws_If_Not_Found()
    {
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("desk1").Returns((Hardware?)null);

        var sut = host.Get<DeleteDesktopUseCase>();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync("desk1"));
    }
}