using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.Desktops;

public class GetDesktopUseCaseTests
{
    [Fact]
    public async Task Returns_Desktop()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("desk1").Returns(new Desktop { Name = "desk1" });

        var useCase = new GetDesktopUseCase(repo);

        var result = await useCase.ExecuteAsync("desk1");

        Assert.NotNull(result);
        Assert.Equal("desk1", result!.Name);
    }

    [Fact]
    public async Task Returns_Null_If_Not_Found()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("desk1").Returns((Hardware?)null);

        var useCase = new GetDesktopUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync("desk1"));

    }
}