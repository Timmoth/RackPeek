using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Server.Nic;
using Xunit;

namespace Tests.Hardware;

public class AddNicUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_nic_when_server_exists()
    {
        var repo = Substitute.For<IHardwareRepository>();
        var server = new Server
        {
            Name = "node01",
            Nics = new List<Nic>()
        };

        repo.GetByNameAsync("node01").Returns(server);

        var sut = new AddNicUseCase(repo);

        await sut.ExecuteAsync("node01", "10GBase-T", 10000, 2);

        Assert.Single(server.Nics);
        Assert.Equal("10GBase-T", server.Nics[0].Type);
        Assert.Equal(10000, server.Nics[0].Speed!.Value);
        Assert.Equal(2, server.Nics[0].Ports!.Value);

        await repo.Received(1).UpdateAsync(Arg.Is<Server>(s =>
            s.Nics.Count == 1 &&
            s.Nics[0].Type == "10GBase-T"
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Initializes_list_when_null()
    {
        var repo = Substitute.For<IHardwareRepository>();
        var server = new Server { Name = "node01", Nics = null };

        repo.GetByNameAsync("node01").Returns(server);

        var sut = new AddNicUseCase(repo);

        await sut.ExecuteAsync("node01", "SFP+", 10000, 1);

        Assert.NotNull(server.Nics);
        Assert.Single(server.Nics);

        await repo.Received(1).UpdateAsync(Arg.Any<Server>());
    }

    [Fact]
    public async Task ExecuteAsync_Does_nothing_when_server_not_found()
    {
        var repo = Substitute.For<IHardwareRepository>();

        repo.GetByNameAsync("node01")
            .Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new AddNicUseCase(repo);

        await sut.ExecuteAsync("node01", "SFP+", 10000, 1);

        await repo.DidNotReceive().UpdateAsync(Arg.Any<Server>());
    }
}