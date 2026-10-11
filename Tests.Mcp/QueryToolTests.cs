using System.Text.Json;
using ModelContextProtocol.Client;
using RackPeek.Domain.Api;
using RackPeek.Domain.Resources.Connections;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Mcp.Tools;

namespace Tests.Mcp;

/// <summary>
///     The read half of the tool surface, over the seed inventory in
///     <see cref="TestData.Seed" />. Assertions state exact expectations — the seed is
///     small enough that anything looser would just be hiding a wrong answer.
/// </summary>
public class QueryToolTests {
    // -- list_resources -----------------------------------------------------------------

    [Fact]
    public async Task Listing_returns_every_resource_with_its_key_facts() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ResourceList list = await client.CallOkAsync<ResourceList>("list_resources");

        Assert.Equal(5, list.Count);

        ResourceRow hostOs = Assert.Single(list.Resources, r => r.Name == "host-os");
        Assert.Equal("System", hostOs.Kind);
        Assert.Equal("10.0.0.5", hostOs.Ip);
        Assert.Equal(["rack-server"], hostOs.RunsOn);
        Assert.Equal("prod", Assert.Contains("env", hostOs.Labels));
    }

    [Theory]
    [InlineData("Server", "rack-server")]
    [InlineData("server", "rack-server")] // kind matching must not be case-sensitive
    [InlineData("Switch", "rack-switch")]
    [InlineData("System", "host-os")]
    public async Task Listing_filters_by_kind(string kind, string expected) {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ResourceList list = await client.CallOkAsync<ResourceList>(
            "list_resources", new Dictionary<string, object?> { ["kind"] = kind });

        ResourceRow row = Assert.Single(list.Resources);
        Assert.Equal(expected, row.Name);
    }

    [Fact]
    public async Task Listing_filters_by_tag_and_label_key() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ResourceList byTag = await client.CallOkAsync<ResourceList>(
            "list_resources", new Dictionary<string, object?> { ["tag"] = "prod" });
        ResourceList byLabel = await client.CallOkAsync<ResourceList>(
            "list_resources", new Dictionary<string, object?> { ["labelKey"] = "env" });

        Assert.Equal("rack-server", Assert.Single(byTag.Resources).Name);
        Assert.Equal("host-os", Assert.Single(byLabel.Resources).Name);
    }

    [Fact]
    public async Task An_unknown_kind_is_an_empty_list_not_an_error() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ResourceList list = await client.CallOkAsync<ResourceList>(
            "list_resources", new Dictionary<string, object?> { ["kind"] = "mainframe" });

        Assert.Equal(0, list.Count);
    }

    // -- get_resource -------------------------------------------------------------------

    [Fact]
    public async Task A_resource_comes_back_as_schema_conformant_yaml_with_its_connections() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ResourceDetail detail = await client.CallOkAsync<ResourceDetail>(
            "get_resource", new Dictionary<string, object?> { ["name"] = "rack-server" });

        Assert.Contains("kind: Server", detail.Yaml);
        Assert.Contains("name: rack-server", detail.Yaml);
        Assert.Contains("rack-switch", detail.Yaml); // the connection's far end
        Assert.Equal(["host-os"], detail.Dependants);
        SchemaAssert.ConformsToSchema(detail.Yaml);
    }

    [Fact]
    public async Task The_yaml_from_get_resource_round_trips_through_upsert_without_phantom_changes() {
        // The read and write halves must agree on the format, or an agent that reads,
        // tweaks nothing and writes back would report changes it never made.
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ResourceDetail detail = await client.CallOkAsync<ResourceDetail>(
            "get_resource", new Dictionary<string, object?> { ["name"] = "host-os" });

        ImportYamlResponse response = await client.CallOkAsync<ImportYamlResponse>(
            "upsert_resources", new Dictionary<string, object?> { ["yaml"] = detail.Yaml, ["dryRun"] = true });

        Assert.Empty(response.Added);
        Assert.Empty(response.Updated);
        Assert.Empty(response.Replaced);
    }

    [Fact]
    public async Task Asking_for_a_resource_that_does_not_exist_is_a_useful_error() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync(
            "get_resource", new Dictionary<string, object?> { ["name"] = "no-such-box" });

        Assert.Contains("no-such-box", error);
        Assert.Contains("not found", error);
    }

    // -- search_resources ---------------------------------------------------------------

    [Fact]
    public async Task Search_finds_by_name_ip_tag_and_label() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        SearchResults byName = await client.CallOkAsync<SearchResults>(
            "search_resources", new Dictionary<string, object?> { ["query"] = "grafana" });
        SearchResults byIp = await client.CallOkAsync<SearchResults>(
            "search_resources", new Dictionary<string, object?> { ["query"] = "10.0.1.9" });

        Assert.Equal("grafana", byName.Matches.First().Name);
        Assert.Equal("prometheus", byIp.Matches.First().Name);
    }

    [Fact]
    public async Task Search_respects_the_max_argument() {
        using var api = new McpFixture(TestData.DemoConfig());
        await using McpClient client = await api.ConnectAsync();

        SearchResults matches = await client.CallOkAsync<SearchResults>(
            "search_resources", new Dictionary<string, object?> { ["query"] = "e", ["max"] = 3 });

        Assert.Equal(3, matches.Matches.Count);
    }

    // -- get_summary ---------------------------------------------------------------------

    [Fact]
    public async Task The_summary_counts_everything_in_one_call() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        InfrastructureSummary summary = await client.CallOkAsync<InfrastructureSummary>("get_summary");

        Assert.Equal(2, summary.Hardware.TotalHardware);
        Assert.Equal(1, summary.Systems.TotalSystems);
        Assert.Equal(2, summary.Services.TotalServices);
        Assert.Equal(1, Assert.Contains("prod", summary.Tags));
        Assert.Equal(1, Assert.Contains("env", summary.Labels));
    }

    // -- get_tree ------------------------------------------------------------------------

    [Fact]
    public async Task The_tree_nests_services_under_systems_under_hardware() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        TreeResult tree = await client.CallOkAsync<TreeResult>("get_tree");

        HardwareTree server = Assert.Single(tree.Hardware, h => h.HardwareName == "rack-server");
        SystemTree system = Assert.Single(server.Systems);
        Assert.Equal("host-os", system.SystemName);
        Assert.Equal(["grafana", "prometheus"], system.Services.OrderBy(s => s, StringComparer.Ordinal));
    }

    [Fact]
    public async Task The_tree_can_be_narrowed_to_one_hardware_resource() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        TreeResult tree = await client.CallOkAsync<TreeResult>(
            "get_tree", new Dictionary<string, object?> { ["hardwareName"] = "rack-switch" });

        Assert.Equal("rack-switch", Assert.Single(tree.Hardware).HardwareName);

        var error = await client.CallErrorAsync(
            "get_tree", new Dictionary<string, object?> { ["hardwareName"] = "no-such-rack" });
        Assert.Contains("not found", error);
    }

    // -- list_connections ----------------------------------------------------------------

    [Fact]
    public async Task Connections_are_listed_whole_and_per_resource() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ConnectionList all = await client.CallOkAsync<ConnectionList>("list_connections");
        ConnectionList forSwitch = await client.CallOkAsync<ConnectionList>(
            "list_connections", new Dictionary<string, object?> { ["resource"] = "rack-switch" });
        ConnectionList forHost = await client.CallOkAsync<ConnectionList>(
            "list_connections", new Dictionary<string, object?> { ["resource"] = "host-os" });

        Connection connection = Assert.Single(all.Connections);
        Assert.Equal("rack-server", connection.A.Resource);
        Assert.Equal("rack-switch", connection.B.Resource);
        Assert.Equal("uplink", connection.Label);

        Assert.Equal(1, forSwitch.Count);
        Assert.Equal(0, forHost.Count);
    }

    // -- get_subnets ---------------------------------------------------------------------

    [Fact]
    public async Task Service_ips_group_into_subnets_and_filter_by_cidr() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        JsonElement grouped = await client.CallOkAsync<JsonElement>("get_subnets");
        JsonElement filtered = await client.CallOkAsync<JsonElement>(
            "get_subnets", new Dictionary<string, object?> { ["cidr"] = "10.0.0.0/24" });

        var subnets = grouped.GetProperty("subnets").EnumerateArray()
            .Select(s => s.GetProperty("cidr").GetString())
            .ToList();
        Assert.Equal(["10.0.0.0/24", "10.0.1.0/24"], subnets);

        var services = filtered.GetProperty("services").EnumerateArray()
            .Select(s => s.GetProperty("name").GetString())
            .ToList();
        Assert.Equal(["grafana"], services);
    }

    [Fact]
    public async Task A_malformed_cidr_is_an_error_that_shows_the_right_shape() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync(
            "get_subnets", new Dictionary<string, object?> { ["cidr"] = "not-a-cidr" });

        Assert.Contains("not-a-cidr", error);
        Assert.Contains("192.168.1.0/24", error);
    }

    // -- get_schema ----------------------------------------------------------------------

    [Fact]
    public async Task The_schema_tool_serves_the_current_published_schema() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        SchemaInfo info = await client.CallOkAsync<SchemaInfo>("get_schema");

        Assert.Equal(4, info.Version);
        Assert.False(string.IsNullOrWhiteSpace(info.Guidance));

        // Byte-for-byte the schema the repo publishes, so the tool cannot drift.
        var published = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "schemas", "schema.v4.json"));
        Assert.Equal(published, info.JsonSchema);
    }

    // -- breadth over the demo inventory ---------------------------------------------------

    [Fact]
    public async Task The_whole_demo_inventory_lists_trees_and_reads_back_conformant_yaml() {
        using var api = new McpFixture(TestData.DemoConfig());
        await using McpClient client = await api.ConnectAsync();

        ResourceList list = await client.CallOkAsync<ResourceList>("list_resources");
        Assert.Equal(45, list.Count);

        TreeResult tree = await client.CallOkAsync<TreeResult>("get_tree");
        Assert.NotEmpty(tree.Hardware);

        foreach (var name in new[] { "proxmox-node01", "pfsense-fw", "plex", "proxmox-cluster-node01" }) {
            ResourceDetail detail = await client.CallOkAsync<ResourceDetail>(
                "get_resource", new Dictionary<string, object?> { ["name"] = name });
            SchemaAssert.ConformsToSchema(detail.Yaml);
        }
    }
}
