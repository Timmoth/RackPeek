using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Api;
using RackPeek.Domain.Persistence;

namespace Tests.Api;

public class InventoryEndpointTests : IClassFixture<WebApplicationFactory<RackPeek.Web.Program>>
{
    private readonly WebApplicationFactory<RackPeek.Web.Program> _factory;

    public InventoryEndpointTests(WebApplicationFactory<RackPeek.Web.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RPK_API_KEY"] = "test-key-123"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<RackPeek.Domain.Persistence.Yaml.ResourceCollection>();
                services.AddScoped<IResourceCollection>(_ => new InMemoryResourceCollection());
            });
        });
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient();
    }

    [Fact]
    public async Task Returns_401_without_api_key()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "srv01"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Returns_401_with_wrong_api_key()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "srv01"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Returns_200_with_valid_api_key()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-key-123");

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            RamGb = 64
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(result);
        Assert.Equal("srv01", result.Hardware.Name);
        Assert.Equal("Server", result.Hardware.Kind);
        Assert.Equal("created", result.Hardware.Action);
    }

    [Fact]
    public async Task Returns_hardware_and_system_in_response()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-key-123");

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "srv02",
            HardwareType = "server",
            Os = "Ubuntu 24.04",
            SystemType = "baremetal"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<InventoryResponse>();
        Assert.NotNull(result);
        Assert.Equal("srv02", result.Hardware.Name);
        Assert.NotNull(result.System);
        Assert.Equal("srv02-system", result.System.Name);
        Assert.Equal("System", result.System.Kind);
    }

    [Fact]
    public async Task Returns_400_for_invalid_request()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-key-123");

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Drives = [new InventoryDrive { Type = "floppy", Size = 1 }]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_503_when_api_key_not_configured()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RPK_API_KEY"] = ""
                });
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<RackPeek.Domain.Persistence.Yaml.ResourceCollection>();
                services.AddScoped<IResourceCollection>(_ => new InMemoryResourceCollection());
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "any-key");

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "srv01"
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Returns_401_when_api_key_has_wrong_case()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "Test-Key-123");

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "srv-case",
            HardwareType = "server"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Returns_400_for_empty_hostname()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-key-123");

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "  ",
            HardwareType = "server"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_400_for_negative_drive_size()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-key-123");

        var response = await client.PostAsJsonAsync("/api/inventory", new InventoryRequest
        {
            Hostname = "srv-negdrive",
            HardwareType = "server",
            Drives = [new InventoryDrive { Type = "ssd", Size = -100 }]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
