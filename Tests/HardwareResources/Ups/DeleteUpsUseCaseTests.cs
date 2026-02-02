using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.UpsUnits;

namespace Tests.HardwareResources.Ups;

public class DeleteUpsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Deletes_ups_when_exists()
    {
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("ups01").Returns(new RackPeek.Domain.Resources.Hardware.Models.Ups { Name = "ups01" });

        var sut = host.Get<DeleteUpsUseCase>();

        await sut.ExecuteAsync("ups01");

        await repo.Received(1).DeleteAsync("ups01");
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_ups_not_found()
    {
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("ups01").Returns((Hardware?)null);

        var sut = host.Get<DeleteUpsUseCase>();

        var ex = await Assert.ThrowsAsync<NotFoundException>(async () =>
            await sut.ExecuteAsync("ups01")
        );

        Assert.Equal("UPS 'ups01' not found.", ex.Message);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }
}