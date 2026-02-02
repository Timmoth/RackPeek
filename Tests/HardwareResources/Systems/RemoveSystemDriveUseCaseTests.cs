using System.ComponentModel.DataAnnotations;
using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.SystemResources.UseCases;

namespace Tests.HardwareResources.Systems;

public class RemoveSystemDriveUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Removes_drive_when_found()
    {
        // Arrange
        var repo = Substitute.For<ISystemRepository>();
        var drive = new Drive { Type = "ssd", Size = 256 };
        var system = new SystemResource
        {
            Name = "sys1",
            Drives = new List<Drive> { drive }
        };

        repo.GetByNameAsync("sys1").Returns(system);

        var sut = new RemoveSystemDriveUseCase(repo);

        // Act
        await sut.ExecuteAsync("sys1", "ssd");

        // Assert
        Assert.Empty(system.Drives);

        await repo.Received(1).UpdateAsync(system);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_system_not_found()
    {
        var repo = Substitute.For<ISystemRepository>();
        repo.GetByNameAsync("sys1").Returns((SystemResource?)null);

        var sut = new RemoveSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("sys1", "ssd")
        );
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_drive_not_found()
    {
        var repo = Substitute.For<ISystemRepository>();
        var system = new SystemResource { Name = "sys1", Drives = new List<Drive>() };

        repo.GetByNameAsync("sys1").Returns(system);

        var sut = new RemoveSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("sys1", "ssd")
        );
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_type_invalid()
    {
        var repo = Substitute.For<ISystemRepository>();
        var sut = new RemoveSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ExecuteAsync("sys1", "")
        );
    }
}
