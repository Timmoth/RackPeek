using ModelContextProtocol.Client;
using RackPeek.Domain.UseCases.Ansible;
using RackPeek.Domain.UseCases.Hosts;
using RackPeek.Domain.UseCases.SSH;

namespace Tests.Mcp;

/// <summary>
///     The exporters render the same seed everywhere, so these tests check the shape a
///     downstream consumer (ansible, ssh, /etc/hosts, mermaid) would actually parse.
/// </summary>
public class ExportToolTests {
    [Fact]
    public async Task The_ansible_inventory_groups_labelled_hosts_in_both_formats() {
        // The generator addresses hosts by their ansible_host/ip/hostname label and
        // emits them under the groups asked for — no grouping, no output.
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var args = new Dictionary<string, object?> { ["groupByTags"] = new[] { "prod" } };

        InventoryResult ini = await client.CallOkAsync<InventoryResult>("export_ansible_inventory", args);
        InventoryResult yaml = await client.CallOkAsync<InventoryResult>(
            "export_ansible_inventory",
            new Dictionary<string, object?>(args) { ["format"] = "Yaml" });

        Assert.Contains("[prod]", ini.InventoryText);
        Assert.Contains("rack-server", ini.InventoryText);
        Assert.Contains("ansible_host=10.0.0.2", ini.InventoryText);
        Assert.Contains("rack-server", yaml.InventoryText);
        Assert.NotEqual(ini.InventoryText, yaml.InventoryText);
    }

    [Fact]
    public async Task The_ssh_config_writes_host_blocks_with_the_chosen_defaults() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        SshExportResult result = await client.CallOkAsync<SshExportResult>(
            "export_ssh_config", new Dictionary<string, object?> { ["defaultUser"] = "admin" });

        Assert.Contains("Host host-os", result.ConfigText);
        Assert.Contains("HostName 10.0.0.5", result.ConfigText);
        Assert.Contains("User admin", result.ConfigText);
    }

    [Fact]
    public async Task The_hosts_file_maps_addresses_to_names_with_an_optional_suffix() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        HostsExportResult plain = await client.CallOkAsync<HostsExportResult>("export_hosts_file");
        HostsExportResult suffixed = await client.CallOkAsync<HostsExportResult>(
            "export_hosts_file", new Dictionary<string, object?> {
                ["domainSuffix"] = "home.lan",
                ["includeLocalhostDefaults"] = false
            });

        Assert.Contains("127.0.0.1 localhost", plain.HostsText);
        Assert.Contains("10.0.0.5 host-os", plain.HostsText);
        Assert.Contains("10.0.0.5 host-os.home.lan", suffixed.HostsText);
        Assert.DoesNotContain("localhost", suffixed.HostsText);
    }

    [Fact]
    public async Task The_mermaid_views_draw_the_physical_and_logical_pictures() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        var physical = await client.CallTextAsync("export_topology_mermaid");
        var logical = await client.CallTextAsync(
            "export_topology_mermaid", new Dictionary<string, object?> { ["view"] = "Logical" });

        // Physical: hardware nodes and the cabled edge between them.
        Assert.Contains("rack-server", physical);
        Assert.Contains("rack-switch", physical);
        Assert.Contains("uplink", physical);

        // Logical: host cards carrying their services; no cabling.
        Assert.Contains("host-os", logical);
        Assert.Contains("grafana", logical);
        Assert.DoesNotContain("uplink", logical);
    }

    [Fact]
    public async Task Exports_over_the_demo_inventory_produce_output_for_every_format() {
        using var api = new McpFixture(TestData.DemoConfig());
        await using McpClient client = await api.ConnectAsync();

        InventoryResult ansible = await client.CallOkAsync<InventoryResult>("export_ansible_inventory");
        SshExportResult ssh = await client.CallOkAsync<SshExportResult>("export_ssh_config");
        HostsExportResult hosts = await client.CallOkAsync<HostsExportResult>("export_hosts_file");
        var mermaid = await client.CallTextAsync("export_topology_mermaid");

        // No grouping asked for, so ansible legitimately answers with a warning
        // rather than hosts; the call itself must still succeed.
        Assert.NotNull(ansible);
        Assert.False(string.IsNullOrWhiteSpace(ssh.ConfigText));
        Assert.Contains("pfsense-fw", mermaid);
        Assert.NotEmpty(hosts.HostsText);
    }
}
