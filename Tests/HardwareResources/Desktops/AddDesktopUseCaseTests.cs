using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.Desktops;

public class AddDesktopUseCaseTests
{
    [Fact]
    public async Task Adds_New_Desktop()
    {
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("desk1").Returns((Hardware?)null);

        var sut = host.Get<AddDesktopUseCase>();

        await sut.ExecuteAsync("desk1");

        await repo.Received().AddAsync(Arg.Is<Desktop>(d => d.Name == "desk1"));
    }

    [Fact]
    public async Task Throws_If_Desktop_Exists()
    {
        var host = new UsecaseTestHost();
        host.ResourceRepo.GetResourceKindAsync("desk1").Returns("Server");

        var sut = host.Get<AddDesktopUseCase>();

        await Assert.ThrowsAsync<ConflictException>(() => sut.ExecuteAsync("desk1"));
    }
}