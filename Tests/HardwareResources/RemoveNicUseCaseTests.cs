using System.ComponentModel.DataAnnotations;
using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Servers.Nics;

namespace Tests.HardwareResources;

public class RemoveNicUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Removes_nic_when_index_valid()
    {
        var repo = Substitute.For<IHardwareRepository>();
        var server = new Server
        {
            Name = "node01",
            Nics = new List<Nic>
            {
                new() { Type = "10GBase-T", Speed = 10000, Ports = 2 },
                new() { Type = "SFP+", Speed = 10000, Ports = 1 }
            }
        };

        repo.GetByNameAsync("node01").Returns(server);

        var sut = new RemoveNicUseCase(repo);

        await sut.ExecuteAsync("node01", 0);

        Assert.Single(server.Nics);
        Assert.Equal("SFP+", server.Nics[0].Type);

        await repo.Received(1).UpdateAsync(Arg.Any<Server>());
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_index_out_of_range()
    {
        var repo = Substitute.For<IHardwareRepository>();
        var server = new Server
        {
            Name = "node01",
            Nics = new List<Nic> { new() }
        };

        repo.GetByNameAsync("node01").Returns(server);

        var sut = new RemoveNicUseCase(repo);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ExecuteAsync("node01", 1));

        await repo.DidNotReceive().UpdateAsync(Arg.Any<Server>());
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_server_not_found()
    {
        var repo = Substitute.For<IHardwareRepository>();

        repo.GetByNameAsync("node01")
            .Returns((Hardware?)null);

        var sut = new RemoveNicUseCase(repo);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("node01", 0)
        );

        await repo.DidNotReceive().UpdateAsync(Arg.Any<Server>());
    }
}