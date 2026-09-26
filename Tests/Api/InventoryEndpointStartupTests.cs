using System.Net.Http.Json;
using RackPeek.Domain.Api;
using Xunit.Abstractions;

namespace Tests.Api;

/// <summary>
///     The inventory API is reached without a browser — by scripts, by CI, and by
///     <c>rpk discover --push</c> on a timer. It therefore has to work on a server that
///     nobody has opened a page on yet.
/// </summary>
public class InventoryEndpointStartupTests(ITestOutputHelper output) : ApiTestBase(output) {
    private const string _existingConfig = """
                                           version: 3
                                           resources:
                                             - kind: Server
                                               name: hand-written-server
                                               notes: documented by hand years ago
                                           connections: []
                                           """;

    [Fact]
    public async Task The_first_request_after_a_restart_does_not_destroy_the_existing_inventory() {
        var configPath = Path.Combine(TempDir, "config.yaml");
        await File.WriteAllTextAsync(configPath, _existingConfig);

        // The very first thing to touch this server is an API call, with no Blazor
        // circuit having ever initialised and therefore no implicit load.
        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new {
            yaml = """
                   version: 3
                   resources:
                     - kind: Server
                       name: newly-discovered-box
                   """,
            mode = "Merge"
        });

        response.EnsureSuccessStatusCode();

        var stored = await File.ReadAllTextAsync(configPath);

        Assert.Contains("hand-written-server", stored);
        Assert.Contains("documented by hand years ago", stored);
        Assert.Contains("newly-discovered-box", stored);
    }

    [Fact]
    public async Task An_existing_resource_is_updated_rather_than_re_added_on_a_cold_server() {
        await File.WriteAllTextAsync(Path.Combine(TempDir, "config.yaml"), _existingConfig);

        HttpClient client = CreateClient(true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new {
            yaml = """
                   version: 3
                   resources:
                     - kind: Server
                       name: hand-written-server
                       notes: updated
                   """,
            mode = "Merge"
        });

        ImportYamlResponse? result = await response.Content.ReadFromJsonAsync<ImportYamlResponse>();

        Assert.Empty(result!.Added);
        Assert.Equal(["hand-written-server"], result.Updated);
    }

    [Fact]
    public async Task An_unreadable_config_does_not_stop_the_server_starting() {
        // The web UI is how someone fixes a broken config, so it has to come up.
        await File.WriteAllTextAsync(
            Path.Combine(TempDir, "config.yaml"),
            "version: 3\nresources:\n  - kind: Server\n    name: [unterminated\n");

        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal("rackpeek", await response.Content.ReadAsStringAsync());
    }
}
