using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.UpsUnits;

namespace Tests.HardwareResources.Ups;

public class GetUpsUnitUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_ups_when_it_exists()
    {
        var repo = Substitute.For<IHardwareRepository>();
        var ups = new RackPeek.Domain.Resources.Hardware.Models.Ups { Name = "ups01" };
        repo.GetByNameAsync("ups01").Returns(ups);

        var sut = new GetUpsUnitUseCase(repo);

        var result = await sut.ExecuteAsync("ups01");

        Assert.NotNull(result);
        Assert.Same(ups, result);
    }

    [Fact]
    public async Task ExecuteAsync_Returns_null_when_hardware_is_not_ups()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server { Name = "node01" });

        var sut = new GetUpsUnitUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync("node01"));

    }
}