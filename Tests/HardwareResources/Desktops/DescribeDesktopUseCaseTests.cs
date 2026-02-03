using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.Resources.Hardware.Models;

namespace Tests.HardwareResources.Desktops;

public class DescribeDesktopUseCaseTests
{
    [Fact]
    public async Task Returns_Description()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("desk1").Returns(new Desktop
        {
            Name = "desk1",
            Model = "Optiplex",
            Cpus = new List<Cpu> { new() },
            Ram = new Ram { Size = 16, Mts = 2666 },
            Drives = new List<Drive> { new() },
            Nics = new List<Nic> { new() },
            Gpus = new List<Gpu> { new() }
        });

        var useCase = new DescribeDesktopUseCase(repo);

        var result = await useCase.ExecuteAsync("desk1");

        Assert.NotNull(result);
        Assert.Equal("desk1", result!.Name);
        Assert.Equal(1, result.CpuCount);
        Assert.Equal(1, result.DriveCount);
        Assert.Equal(1, result.NicCount);
        Assert.Equal(1, result.GpuCount);
    }

    [Fact]
    public async Task Returns_Null_If_Not_Found()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("desk1").Returns((Hardware?)null);

        var useCase = new DescribeDesktopUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync("desk1"));

    }
}