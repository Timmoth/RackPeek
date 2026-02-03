using System.ComponentModel.DataAnnotations;
using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.SystemResources.UseCases;

namespace Tests.HardwareResources.Systems;

public class AddSystemDriveUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Adds_drive_when_system_exists()
    {
        // Arrange
        var repo = Substitute.For<ISystemRepository>();
        var system = new SystemResource
        {
            Name = "sys1",
            Drives = new List<Drive>()
        };

        repo.GetByNameAsync("sys1").Returns(system);

        var sut = new AddSystemDriveUseCase(repo);

        // Act
        await sut.ExecuteAsync("sys1", "ssd", 512);

        // Assert
        Assert.Single(system.Drives);
        Assert.Equal("ssd", system.Drives[0].Type);
        Assert.Equal(512, system.Drives[0].Size);

        await repo.Received(1).UpdateAsync(Arg.Is<SystemResource>(s =>
            s.Name == "sys1" &&
            s.Drives.Count == 1 &&
            s.Drives[0].Type == "ssd" &&
            s.Drives[0].Size == 512
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Initializes_drive_list_when_null()
    {
        // Arrange
        var repo = Substitute.For<ISystemRepository>();
        var system = new SystemResource
        {
            Name = "sys1",
            Drives = null
        };

        repo.GetByNameAsync("sys1").Returns(system);

        var sut = new AddSystemDriveUseCase(repo);

        // Act
        await sut.ExecuteAsync("sys1", "sata", 500);

        // Assert
        Assert.NotNull(system.Drives);
        Assert.Single(system.Drives);

        await repo.Received(1).UpdateAsync(Arg.Is<SystemResource>(s =>
            s.Drives != null &&
            s.Drives.Count == 1 &&
            s.Drives[0].Type == "sata"
        ));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_system_not_found()
    {
        // Arrange
        var repo = Substitute.For<ISystemRepository>();
        repo.GetByNameAsync("sys1").Returns((SystemResource?)null);

        var sut = new AddSystemDriveUseCase(repo);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("sys1", "ssd", 512)
        );
    }
    
    [Fact]
    public async Task ExecuteAsync_Throws_when_type_invalid()
    {
        var repo = Substitute.For<ISystemRepository>();
        var sut = new AddSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ExecuteAsync("sys1", "", 512)
        );
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_size_negative()
    {
        var repo = Substitute.For<ISystemRepository>();
        var sut = new AddSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ExecuteAsync("sys1", "ssd", -1)
        );
    }
}
