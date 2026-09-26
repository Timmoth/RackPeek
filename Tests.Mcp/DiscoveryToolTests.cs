using ModelContextProtocol.Client;
using RackPeek.Mcp.Tools;

namespace Tests.Mcp;

/// <summary>
///     The discovery tools against fake engines answering on real sockets: preview
///     returns reviewable YAML, apply merges it, and a second run changes nothing —
///     the discovery-id identity contract, observed through the MCP surface.
/// </summary>
public class DiscoveryToolTests {
    // -- discover_docker ----------------------------------------------------------------

    [Fact]
    public async Task Docker_discovery_previews_reachable_containers_as_conformant_yaml() {
        await using FakeHttpServer engine = await FakeHttpServer.StartDockerEngineAsync();
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        DiscoveryResult result = await client.CallOkAsync<DiscoveryResult>(
            "discover_docker", new Dictionary<string, object?> {
                ["dockerHost"] = $"tcp://{engine.Host}",
                ["hostName"] = "nas01"
            });

        // The fixture holds six containers; redis publishes nothing and pgadmin only
        // binds loopback, so four become services.
        Assert.Equal(4, result.ResourceCount);
        Assert.Equal(2, result.Skipped);
        Assert.Null(result.Applied);

        Assert.Contains("jellyfin", result.Yaml);
        Assert.Contains("wireguard", result.Yaml);
        Assert.DoesNotContain("redis", result.Yaml);
        Assert.Contains("runsOn", result.Yaml);
        Assert.Contains("nas01", result.Yaml);
        SchemaAssert.ConformsToSchema(result.Yaml);

        // Preview writes nothing.
        Assert.DoesNotContain("jellyfin", api.StoredYaml);
    }

    [Fact]
    public async Task Applying_docker_discovery_merges_and_a_second_run_changes_nothing() {
        await using FakeHttpServer engine = await FakeHttpServer.StartDockerEngineAsync();
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        var args = new Dictionary<string, object?> {
            ["dockerHost"] = $"tcp://{engine.Host}",
            ["hostName"] = "nas01",
            ["apply"] = true
        };

        DiscoveryResult first = await client.CallOkAsync<DiscoveryResult>("discover_docker", args);

        Assert.NotNull(first.Applied);
        Assert.Equal(4, first.Applied.Added.Count);
        Assert.Contains("jellyfin", api.StoredYaml);
        Assert.Contains("discoveryId: rpk1:docker:", api.StoredYaml);
        SchemaAssert.ConformsToSchema(api.StoredYaml);

        // Same engine, same answers: the ids line everything up, nothing duplicates.
        DiscoveryResult second = await client.CallOkAsync<DiscoveryResult>("discover_docker", args);

        Assert.NotNull(second.Applied);
        Assert.Empty(second.Applied.Added);
        Assert.Empty(second.Applied.Updated);
    }

    [Fact]
    public async Task Discovered_services_survive_a_user_rename_on_the_next_apply() {
        // The identity contract, end to end: the user renames a discovered service,
        // discovery runs again, and the rename sticks because the id matches.
        await using FakeHttpServer engine = await FakeHttpServer.StartDockerEngineAsync();
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        var args = new Dictionary<string, object?> {
            ["dockerHost"] = $"tcp://{engine.Host}",
            ["hostName"] = "nas01",
            ["apply"] = true
        };

        await client.CallOkAsync<DiscoveryResult>("discover_docker", args);

        await client.CallTextAsync("rename_resource", new Dictionary<string, object?> {
            ["name"] = "jellyfin",
            ["newName"] = "media-jellyfin"
        });

        DiscoveryResult again = await client.CallOkAsync<DiscoveryResult>("discover_docker", args);

        Assert.NotNull(again.Applied);
        Assert.Empty(again.Applied.Added); // not re-added under the old name
        Assert.Contains("media-jellyfin", api.StoredYaml);
    }

    [Fact]
    public async Task An_unreachable_docker_engine_is_a_clean_error_naming_the_endpoint() {
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync("discover_docker",
            new Dictionary<string, object?> { ["dockerHost"] = "tcp://127.0.0.1:1" });

        Assert.Contains("Could not reach Docker", error);
        Assert.Contains("tcp://127.0.0.1:1", error);
    }

    [Fact]
    public async Task A_malformed_docker_endpoint_is_rejected_before_any_io() {
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync("discover_docker",
            new Dictionary<string, object?> { ["dockerHost"] = "%%not-an-endpoint%%" });

        Assert.Contains("not a usable Docker endpoint", error);
    }

    // -- discover_proxmox ---------------------------------------------------------------

    private static Dictionary<string, string?> PveCredentials() => new() {
        ["RPK_PVE_TOKEN_ID"] = "root@pam!rackpeek",
        ["RPK_PVE_TOKEN_SECRET"] = "secret-uuid"
    };

    [Fact]
    public async Task Proxmox_discovery_previews_nodes_and_guests_wired_together() {
        await using FakeHttpServer pve = await FakeHttpServer.StartProxmoxAsync();
        using var api = new McpFixture(extraConfig: PveCredentials());
        await using McpClient client = await api.ConnectAsync();

        DiscoveryResult result = await client.CallOkAsync<DiscoveryResult>(
            "discover_proxmox", new Dictionary<string, object?> { ["host"] = pve.BaseUrl });

        Assert.Null(result.Applied);
        SchemaAssert.ConformsToSchema(result.Yaml);

        // Two fixture nodes, each a Server plus its hypervisor System...
        Assert.Contains("pve01", result.Yaml);
        Assert.Contains("pve02", result.Yaml);
        Assert.Contains("kind: Server", result.Yaml);
        Assert.Contains("hypervisor", result.Yaml);

        // ...and guests parented onto them, deduped by vmid even though both fake
        // nodes reported the same guest lists (the in-flight migration case).
        Assert.Contains("docker-01", result.Yaml);
        Assert.Contains("pihole", result.Yaml);
        Assert.Single(
            result.Yaml.Split(Environment.NewLine),
            l => l.Contains("name: pihole"));
    }

    [Fact]
    public async Task Applying_proxmox_discovery_persists_the_estate() {
        await using FakeHttpServer pve = await FakeHttpServer.StartProxmoxAsync();
        using var api = new McpFixture(extraConfig: PveCredentials());
        await using McpClient client = await api.ConnectAsync();

        DiscoveryResult result = await client.CallOkAsync<DiscoveryResult>(
            "discover_proxmox",
            new Dictionary<string, object?> { ["host"] = pve.BaseUrl, ["apply"] = true });

        Assert.NotNull(result.Applied);
        Assert.NotEmpty(result.Applied.Added);
        Assert.Contains("pve01", api.StoredYaml);
        Assert.Contains("discoveryId: rpk1:pve:", api.StoredYaml);
        SchemaAssert.ConformsToSchema(api.StoredYaml);
    }

    [Fact]
    public async Task Missing_proxmox_credentials_point_at_the_exact_settings_to_set() {
        using var api = new McpFixture(); // no RPK_PVE_* configured
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync("discover_proxmox",
            new Dictionary<string, object?> { ["host"] = "https://pve.lan:8006" });

        Assert.Contains("RPK_PVE_TOKEN_ID", error);
        Assert.Contains("RPK_PVE_TOKEN_SECRET", error);
    }

    [Fact]
    public async Task An_unreachable_proxmox_host_is_a_clean_error_naming_the_endpoint() {
        using var api = new McpFixture(extraConfig: PveCredentials());
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync("discover_proxmox",
            new Dictionary<string, object?> { ["host"] = "http://127.0.0.1:1" });

        Assert.Contains("Could not read", error);
        Assert.Contains("http://127.0.0.1:1", error);
    }
}
