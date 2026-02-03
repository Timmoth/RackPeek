using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.UpsUnits;

namespace Tests.HardwareResources.Ups;

public class AddUpsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_new_ups_when_not_exists()
    {
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("ups01").Returns((Hardware?)null);

        var sut = host.Get<AddUpsUseCase>();

        await sut.ExecuteAsync("ups01");

        await repo.Received(1).AddAsync(Arg.Is<RackPeek.Domain.Resources.Hardware.Models.Ups>(u =>
            u.Name == "ups01"
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_ups_already_exists()
    {
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        host.ResourceRepo.GetResourceKindAsync("ups01").Returns("Server");

        var sut = host.Get<AddUpsUseCase>();
        
        var ex = await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.ExecuteAsync("ups01")
        );

        await repo.DidNotReceive().AddAsync(Arg.Any<RackPeek.Domain.Resources.Hardware.Models.Ups>());
    }
}