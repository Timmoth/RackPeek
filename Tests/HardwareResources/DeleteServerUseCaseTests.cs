using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.HardwareResources;

public class UsecaseTestHost
{
    private readonly ServiceCollection _sc;

    public UsecaseTestHost()
    {
        HardwareRepo = Substitute.For<IHardwareRepository>();
        SystemRepo = Substitute.For<ISystemRepository>();
        ServiceRepo = Substitute.For<IServiceRepository>();
        _sc = new ServiceCollection();
        _sc.AddSingleton<IHardwareRepository>(HardwareRepo);
        _sc.AddSingleton<ISystemRepository>(SystemRepo);
        _sc.AddSingleton<IServiceRepository>(ServiceRepo);
    }

    public IHardwareRepository HardwareRepo { get; set; }
    public ISystemRepository SystemRepo { get; set; }
    public IServiceRepository ServiceRepo { get; set; }

    public T Get<T>() where T : notnull
    {
        _sc.AddSingleton(typeof(T));
        var sp = _sc.BuildServiceProvider();
        return sp.GetRequiredService<T>();
    }
}

public class DeleteServerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Deletes_server_when_exists()
    {
        // Arrange
        var host = new UsecaseTestHost();
        host.HardwareRepo.GetByNameAsync("node01").Returns(new Server { Name = "node01" });

        var sut = host.Get<DeleteServerUseCase>();

        // Act
        await sut.ExecuteAsync("node01");

        // Assert
        await host.HardwareRepo.Received(1).DeleteAsync("node01");
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_server_not_found()
    {
        // Arrange
        var host = new UsecaseTestHost();
        var repo = host.HardwareRepo;
        repo.GetByNameAsync("node01").Returns((Hardware?)null);

        var sut = host.Get<DeleteServerUseCase>();

        // Act
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("node01")
        );

        // Assert
        Assert.Equal("Server 'node01' not found.", ex.Message);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }
}