using NSubstitute;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Server;

namespace Tests.Hardware;

public class AddServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_new_server_when_not_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns((RackPeek.Domain.Resources.Hardware.Models.Hardware?)null);

        var sut = new AddServerUseCase(repo);

        // Act
        await sut.ExecuteAsync(
            name: "node01"
        );

        // Assert
        await repo.Received(1).AddAsync(Arg.Is<Server>(s =>
            s.Name == "node01"
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_if_server_already_exists()
    {
        // Arrange
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("node01").Returns(new Server { Name = "node01" });

        var sut = new AddServerUseCase(repo);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.ExecuteAsync(
                name: "node01"
            )
        );

        // Assert
        Assert.Equal("Server 'node01' already exists.", ex.Message);
        await repo.DidNotReceive().AddAsync(Arg.Any<Server>());
    }
}