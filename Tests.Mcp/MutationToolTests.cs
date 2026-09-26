using ModelContextProtocol.Client;
using RackPeek.Domain.Api;
using RackPeek.Domain.Resources.Connections;
using RackPeek.Mcp.Tools;

namespace Tests.Mcp;

/// <summary>
///     The write half of the tool surface. Every assertion here is made against what
///     actually lands in config.yaml — the file is the product, not the tool response.
/// </summary>
public class MutationToolTests {
    private const string _newServerYaml =
        """
        version: 4
        resources:
          - kind: Server
            name: new-server
            ram:
              size: 64
            ports:
              - type: rj45
                speed: 1
                count: 2
        """;

    // -- upsert_resources ----------------------------------------------------------------

    [Fact]
    public async Task Upserting_a_new_resource_persists_it_as_schema_conformant_yaml() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ImportYamlResponse response = await client.CallOkAsync<ImportYamlResponse>(
            "upsert_resources", new Dictionary<string, object?> { ["yaml"] = _newServerYaml });

        Assert.Equal(["new-server"], response.Added);
        Assert.Contains("new-server", api.StoredYaml);
        SchemaAssert.ConformsToSchema(api.StoredYaml);
    }

    [Fact]
    public async Task A_dry_run_reports_the_diff_but_writes_nothing() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var before = api.StoredYaml;

        ImportYamlResponse response = await client.CallOkAsync<ImportYamlResponse>(
            "upsert_resources",
            new Dictionary<string, object?> { ["yaml"] = _newServerYaml, ["dryRun"] = true });

        Assert.Equal(["new-server"], response.Added);
        Assert.Contains("new-server", Assert.Contains("new-server", response.NewYaml));
        Assert.Equal(before, api.StoredYaml);
    }

    [Fact]
    public async Task Merge_updates_fields_and_reports_old_and_new_yaml() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ImportYamlResponse response = await client.CallOkAsync<ImportYamlResponse>(
            "upsert_resources", new Dictionary<string, object?> {
                ["yaml"] =
                    """
                    version: 4
                    resources:
                      - kind: System
                        name: host-os
                        cores: 16
                    """
            });

        Assert.Equal(["host-os"], response.Updated);
        Assert.Contains("cores: 8", response.OldYaml["host-os"]);
        Assert.Contains("cores: 16", response.NewYaml["host-os"]);
        Assert.Contains("cores: 16", api.StoredYaml);

        // Merge only adds and updates — everything not mentioned stays.
        Assert.Contains("env: prod", api.StoredYaml);
        Assert.Contains("ip: 10.0.0.5", api.StoredYaml);
    }

    [Fact]
    public async Task Replace_mode_swaps_the_resource_in_wholesale() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        ImportYamlResponse response = await client.CallOkAsync<ImportYamlResponse>(
            "upsert_resources", new Dictionary<string, object?> {
                ["yaml"] =
                    """
                    version: 4
                    resources:
                      - kind: System
                        name: host-os
                        type: vm
                        os: alpine
                    """,
                ["mode"] = "Replace"
            });

        Assert.Equal(["host-os"], response.Replaced);
        Assert.Contains("os: alpine", api.StoredYaml);
        Assert.DoesNotContain("env: prod", api.StoredYaml); // replaced, so the old labels are gone

        // Replace swaps the named resource only; the rest of the file is untouched.
        Assert.Contains("grafana", api.StoredYaml);
    }

    [Theory]
    [InlineData("not: [valid", "Import failed")]
    [InlineData("version: 4", "resources")]
    [InlineData("", "Invalid input")]
    public async Task Broken_documents_are_errors_that_name_the_problem(string yaml, string expected) {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var before = api.StoredYaml;
        var error = await client.CallErrorAsync(
            "upsert_resources", new Dictionary<string, object?> { ["yaml"] = yaml });

        Assert.Contains(expected, error);
        Assert.Equal(before, api.StoredYaml);
    }

    [Fact]
    public async Task The_first_mcp_write_after_a_restart_does_not_destroy_the_existing_inventory() {
        // Same guarantee the inventory API pins down: the server loads the config before
        // serving, so a fresh boot's first merge happens against the user's file rather
        // than an empty collection (which would persist and wipe everything else).
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        await client.CallOkAsync<ImportYamlResponse>(
            "upsert_resources", new Dictionary<string, object?> { ["yaml"] = _newServerYaml });

        Assert.Contains("rack-server", api.StoredYaml);
        Assert.Contains("grafana", api.StoredYaml);
        Assert.Contains("new-server", api.StoredYaml);
    }

    // -- delete_resource -----------------------------------------------------------------

    [Fact]
    public async Task Deleting_hardware_detaches_dependants_and_unplugs_its_connections() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        await client.CallTextAsync(
            "delete_resource", new Dictionary<string, object?> { ["name"] = "rack-server" });

        var stored = api.StoredYaml;
        Assert.DoesNotContain("rack-server", stored);
        SchemaAssert.ConformsToSchema(stored);

        ConnectionList connections = await client.CallOkAsync<ConnectionList>("list_connections");
        Assert.Equal(0, connections.Count);

        ResourceDetail hostOs = await client.CallOkAsync<ResourceDetail>(
            "get_resource", new Dictionary<string, object?> { ["name"] = "host-os" });
        Assert.DoesNotContain("rack-server", hostOs.Yaml);
    }

    [Fact]
    public async Task Deleting_something_that_does_not_exist_is_an_error() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync(
            "delete_resource", new Dictionary<string, object?> { ["name"] = "ghost" });

        Assert.Contains("ghost", error);
    }

    // -- rename_resource -----------------------------------------------------------------

    [Fact]
    public async Task Renaming_rewrites_runs_on_links_and_connection_endpoints() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        await client.CallTextAsync("rename_resource", new Dictionary<string, object?> {
            ["name"] = "rack-server",
            ["newName"] = "compute-01"
        });

        var stored = api.StoredYaml;
        Assert.DoesNotContain("rack-server", stored);
        SchemaAssert.ConformsToSchema(stored);

        ResourceDetail hostOs = await client.CallOkAsync<ResourceDetail>(
            "get_resource", new Dictionary<string, object?> { ["name"] = "host-os" });
        Assert.Contains("compute-01", hostOs.Yaml);

        ConnectionList connections = await client.CallOkAsync<ConnectionList>("list_connections");
        Assert.Equal("compute-01", Assert.Single(connections.Connections).A.Resource);
    }

    [Fact]
    public async Task Renaming_onto_a_taken_name_is_a_conflict() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync("rename_resource", new Dictionary<string, object?> {
            ["name"] = "rack-server",
            ["newName"] = "rack-switch"
        });

        Assert.Contains("already exists", error);
    }

    // -- clone_resource ------------------------------------------------------------------

    [Fact]
    public async Task A_clone_copies_the_kind_specific_fields_but_never_the_discovery_id() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        await client.CallTextAsync("clone_resource", new Dictionary<string, object?> {
            ["name"] = "rack-server",
            ["cloneName"] = "compute-02"
        });

        ResourceDetail clone = await client.CallOkAsync<ResourceDetail>(
            "get_resource", new Dictionary<string, object?> { ["name"] = "compute-02" });

        Assert.Contains("kind: Server", clone.Yaml);
        // The deep copy went through the concrete Server type: the ports survived.
        Assert.Contains("ports:", clone.Yaml);
        Assert.Contains("count: 4", clone.Yaml);
        // A discoveryId names one machine; a copy of its card is not that machine.
        Assert.DoesNotContain("discoveryId", clone.Yaml);
        SchemaAssert.ConformsToSchema(api.StoredYaml);
    }

    [Fact]
    public async Task Cloning_guards_both_names() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        Assert.Contains("not found", await client.CallErrorAsync("clone_resource",
            new Dictionary<string, object?> { ["name"] = "ghost", ["cloneName"] = "copy" }));

        Assert.Contains("already exists", await client.CallErrorAsync("clone_resource",
            new Dictionary<string, object?> { ["name"] = "rack-server", ["cloneName"] = "rack-switch" }));
    }

    // -- edit_tags / edit_labels ---------------------------------------------------------

    [Fact]
    public async Task Tags_can_be_added_and_removed_in_one_call() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        TagsResult result = await client.CallOkAsync<TagsResult>("edit_tags",
            new Dictionary<string, object?> {
                ["name"] = "rack-server",
                ["add"] = new[] { "rack-a", "critical" },
                ["remove"] = new[] { "prod" }
            });

        Assert.Equal(["rack-a", "critical"], result.Tags);
        Assert.Contains("rack-a", api.StoredYaml);
        Assert.DoesNotContain("- prod", api.StoredYaml);
    }

    [Fact]
    public async Task Labels_can_be_set_overwritten_and_removed() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        LabelsResult result = await client.CallOkAsync<LabelsResult>("edit_labels",
            new Dictionary<string, object?> {
                ["name"] = "host-os",
                ["set"] = new Dictionary<string, string> { ["env"] = "staging", ["owner"] = "tim" },
                ["remove"] = new[] { "owner" }
            });

        Assert.Equal("staging", Assert.Contains("env", result.Labels));
        Assert.DoesNotContain("owner", result.Labels.Keys);
        Assert.Contains("env: staging", api.StoredYaml);
    }

    [Fact]
    public async Task Editing_tags_or_labels_needs_something_to_do() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        Assert.Contains("at least one", await client.CallErrorAsync("edit_tags",
            new Dictionary<string, object?> { ["name"] = "rack-server" }));

        Assert.Contains("at least one", await client.CallErrorAsync("edit_labels",
            new Dictionary<string, object?> { ["name"] = "host-os" }));
    }

    // -- add_connection / remove_connection ------------------------------------------------

    private static Dictionary<string, object?> Connect(
        string a, int groupA, int indexA, string b, int groupB, int indexB) => new() {
            ["resourceA"] = a,
            ["portGroupA"] = groupA,
            ["portIndexA"] = indexA,
            ["resourceB"] = b,
            ["portGroupB"] = groupB,
            ["portIndexB"] = indexB
        };

    [Fact]
    public async Task Connecting_two_free_ports_is_persisted() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        await client.CallTextAsync("add_connection",
            Connect("rack-server", 0, 1, "rack-switch", 0, 2));

        ConnectionList connections = await client.CallOkAsync<ConnectionList>("list_connections");
        Assert.Equal(2, connections.Count);
        SchemaAssert.ConformsToSchema(api.StoredYaml);
    }

    [Fact]
    public async Task Connecting_an_occupied_port_replaces_what_was_plugged_into_it() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        // rack-server port 0/0 is already cabled to rack-switch 0/0 in the seed.
        await client.CallTextAsync("add_connection",
            Connect("rack-server", 0, 0, "rack-switch", 0, 5));

        ConnectionList connections = await client.CallOkAsync<ConnectionList>("list_connections");
        Connection connection = Assert.Single(connections.Connections);
        Assert.Equal(5, connection.B.PortIndex);
    }

    [Fact]
    public async Task Impossible_connections_are_named_errors() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        Assert.Contains("itself", await client.CallErrorAsync("add_connection",
            Connect("rack-server", 0, 0, "rack-server", 0, 0)));

        Assert.Contains("no ports", await client.CallErrorAsync("add_connection",
            Connect("host-os", 0, 0, "rack-switch", 0, 1)));

        Assert.Contains("not found", await client.CallErrorAsync("add_connection",
            Connect("rack-server", 0, 99, "rack-switch", 0, 1)));
    }

    [Fact]
    public async Task Removing_a_connection_unplugs_the_port_and_removing_again_is_a_no_op() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var args = new Dictionary<string, object?> {
            ["resource"] = "rack-switch",
            ["portGroup"] = 0,
            ["portIndex"] = 0
        };

        await client.CallTextAsync("remove_connection", args);
        await client.CallTextAsync("remove_connection", args); // idempotent

        ConnectionList connections = await client.CallOkAsync<ConnectionList>("list_connections");
        Assert.Equal(0, connections.Count);
        Assert.DoesNotContain("portIndex", api.StoredYaml);
    }
}
