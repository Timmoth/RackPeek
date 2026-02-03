using System.ComponentModel.DataAnnotations;
using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.SystemResources.UseCases;

namespace Tests.HardwareResources.Systems;

public class UpdateSystemDriveUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Updates_drive_when_found()
    {
        // Arrange
        var repo = Substitute.For<ISystemRepository>();
        var system = new SystemResource
        {
            Name = "sys1",
            Drives = new List<Drive> { new() { Type = "ssd", Size = 256 } }
        };

        repo.GetByNameAsync("sys1").Returns(system);

        var sut = new UpdateSystemDriveUseCase(repo);

        // Act
        await sut.ExecuteAsync("sys1", 0, "nvme", 512);

        // Assert
        Assert.Equal("nvme", system.Drives[0].Type);
        Assert.Equal(512, system.Drives[0].Size);

        await repo.Received(1).UpdateAsync(system);
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_system_not_found()
    {
        var repo = Substitute.For<ISystemRepository>();
        repo.GetByNameAsync("sys1").Returns((SystemResource?)null);

        var sut = new UpdateSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("sys1", 0, "ssd", 512)
        );
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_drive_not_found()
    {
        var repo = Substitute.For<ISystemRepository>();
        var system = new SystemResource { Name = "sys1", Drives = new List<Drive>() };

        repo.GetByNameAsync("sys1").Returns(system);

        var sut = new UpdateSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ExecuteAsync("sys1", 0, "ssd", 512)
        );
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_type_invalid()
    {
        var repo = Substitute.For<ISystemRepository>();
        var sut = new UpdateSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ExecuteAsync("sys1", 0, "", 512)
        );
    }

    [Fact]
    public async Task ExecuteAsync_Throws_when_size_negative()
    {
        var repo = Substitute.For<ISystemRepository>();
        var sut = new UpdateSystemDriveUseCase(repo);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ExecuteAsync("sys1", 0, "ssd", -1)
        );
    }
}
