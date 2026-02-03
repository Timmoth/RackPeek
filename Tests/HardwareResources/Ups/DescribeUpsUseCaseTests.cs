using NSubstitute;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Hardware.UpsUnits;

namespace Tests.HardwareResources.Ups;

public class DescribeUpsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_null_when_ups_not_found()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ups01").Returns((Hardware?)null);

        var sut = new DescribeUpsUseCase(repo);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.ExecuteAsync("ups01"));

    }

    [Fact]
    public async Task ExecuteAsync_Returns_description_when_ups_exists()
    {
        var repo = Substitute.For<IHardwareRepository>();
        repo.GetByNameAsync("ups01").Returns(new RackPeek.Domain.Resources.Hardware.Models.Ups
        {
            Name = "ups01",
            Model = "APC-1500",
            Va = 1500
        });

        var sut = new DescribeUpsUseCase(repo);

        var result = await sut.ExecuteAsync("ups01");

        Assert.NotNull(result);
        Assert.Equal("ups01", result.Name);
        Assert.Equal("APC-1500", result.Model);
        Assert.Equal(1500, result.Va);
    }
}