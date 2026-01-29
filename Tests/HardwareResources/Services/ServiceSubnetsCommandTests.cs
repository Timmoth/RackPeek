using NSubstitute;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Services.UseCases;

namespace Tests.HardwareResources.Services;

public class ServiceSubnetsUseCaseTests
{
    [Fact]
    public async Task Filter_mode_returns_services_in_cidr_sorted_by_ip()
    {
        var repo = Substitute.For<IServiceRepository>();

        repo.GetAllAsync().Returns(new List<Service>
        {
            new()
            {
                Name = "svc1",
                Network = new Network { Ip = "192.168.10.10" },
                RunsOn = "hostA"
            },
            new()
            {
                Name = "svc2",
                Network = new Network { Ip = "192.168.10.20" },
                RunsOn = "hostB"
            },
            new()
            {
                Name = "svc3",
                Network = new Network { Ip = "10.0.0.5" },
                RunsOn = "hostC"
            }
        });

        var sut = new ServiceSubnetsUseCase(repo);

        var result = await sut.ExecuteAsync("192.168.10.0/24", null, CancellationToken.None);

        Assert.False(result.IsInvalidCidr);
        Assert.Equal("192.168.10.0/24", result.FilteredCidr);

        Assert.Equal(2, result.Services.Count);

        // Sorted by IP
        Assert.Equal("svc1", result.Services[0].Name);
        Assert.Equal("svc2", result.Services[1].Name);

        Assert.Equal("192.168.10.10", result.Services[0].Ip);
        Assert.Equal("192.168.10.20", result.Services[1].Ip);
    }

    [Fact]
    public async Task Invalid_cidr_returns_error_result()
    {
        var repo = Substitute.For<IServiceRepository>();
        repo.GetAllAsync().Returns(new List<Service>());

        var sut = new ServiceSubnetsUseCase(repo);

        var result = await sut.ExecuteAsync("not-a-cidr", null, CancellationToken.None);

        Assert.True(result.IsInvalidCidr);
        Assert.Equal("not-a-cidr", result.InvalidCidrValue);
    }

    [Fact]
    public async Task Subnet_discovery_groups_services_by_prefix()
    {
        var repo = Substitute.For<IServiceRepository>();

        repo.GetAllAsync().Returns(new List<Service>
        {
            new() { Name = "svc1", Network = new Network { Ip = "192.168.1.10" } },
            new() { Name = "svc2", Network = new Network { Ip = "192.168.1.20" } },
            new() { Name = "svc3", Network = new Network { Ip = "192.168.2.5" } },
            new() { Name = "svc4", Network = new Network { Ip = "10.0.0.1" } }
        });

        var sut = new ServiceSubnetsUseCase(repo);

        var result = await sut.ExecuteAsync(null, 24, CancellationToken.None);

        Assert.False(result.IsInvalidCidr);
        Assert.Equal(3, result.Subnets.Count);

        Assert.Contains(result.Subnets, s => s.Cidr == "10.0.0.0/24" && s.Count == 1);
        Assert.Contains(result.Subnets, s => s.Cidr == "192.168.1.0/24" && s.Count == 2);
        Assert.Contains(result.Subnets, s => s.Cidr == "192.168.2.0/24" && s.Count == 1);
    }
}