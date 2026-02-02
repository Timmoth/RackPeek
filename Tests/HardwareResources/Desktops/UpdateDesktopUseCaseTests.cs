using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.Desktops;

public class UpdateDesktopUseCaseTests
{
    [Fact]
    public async Task Updates_Model()
    {
        var repo = Substitute.For<IHardwareRepository>();
        var desktop = new Desktop { Name = "desk1" };
        repo.GetByNameAsync("desk1").Returns(desktop);

        var useCase = new UpdateDesktopUseCase(repo);

        await useCase.ExecuteAsync("desk1", "Optiplex");

        Assert.Equal("Optiplex", desktop.Model);
        await repo.Received().UpdateAsync(desktop);
    }

    [Fact]
    public async Task Throws_If_Not_Found()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("desk1").Returns((Hardware?)null);

        var useCase = new UpdateDesktopUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync("desk1", "Optiplex"));
    }
}