using ModelContextProtocol;
using ModelContextProtocol.Client;
using RackPeek.Mcp.Tools;

namespace Tests.Mcp;

/// <summary>
///     The protocol surface itself: the handshake identifies the server, every tool is
///     advertised with enough description for an agent to use it unseen, and a bad tool
///     name is an error rather than a hang or a crash.
/// </summary>
public class ProtocolTests {
    /// <summary>Every tool the server ships. A rename here is a breaking change for clients.</summary>
    public static readonly string[] ExpectedTools = [
        "list_resources", "get_resource", "search_resources", "get_summary", "get_tree",
        "list_connections", "get_subnets", "get_schema",
        "upsert_resources", "delete_resource", "rename_resource", "clone_resource",
        "edit_tags", "edit_labels", "add_connection", "remove_connection",
        "export_ansible_inventory", "export_ssh_config", "export_hosts_file", "export_topology_mermaid",
        "git_status", "git_commit",
        "discover_docker", "discover_proxmox"
    ];

    [Fact]
    public async Task The_handshake_identifies_the_server_by_name_and_version() {
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        Assert.Equal("rackpeek", client.ServerInfo.Name);
        Assert.False(string.IsNullOrWhiteSpace(client.ServerInfo.Version));
    }

    [Fact]
    public async Task Every_expected_tool_is_advertised_and_nothing_else() {
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        IList<McpClientTool> tools = await client.ListToolsAsync();

        Assert.Equal(
            ExpectedTools.OrderBy(t => t, StringComparer.Ordinal),
            tools.Select(t => t.Name).OrderBy(t => t, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Every_tool_carries_a_description_an_agent_can_act_on() {
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        IList<McpClientTool> tools = await client.ListToolsAsync();

        foreach (McpClientTool tool in tools)
            Assert.False(
                string.IsNullOrWhiteSpace(tool.Description),
                $"Tool '{tool.Name}' has no description.");
    }

    [Fact]
    public async Task Calling_a_tool_that_does_not_exist_is_a_clean_error() {
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        await Assert.ThrowsAnyAsync<McpException>(async () =>
            await client.CallToolAsync("does_not_exist"));
    }

    [Fact]
    public async Task Two_clients_can_talk_to_the_same_server_at_once() {
        // Stateless streamable HTTP: no session to collide on.
        using var api = new McpFixture();
        await using McpClient first = await api.ConnectAsync();
        await using McpClient second = await api.ConnectAsync();

        await Task.WhenAll(
            first.CallOkAsync<ResourceList>("list_resources"),
            second.CallOkAsync<ResourceList>("list_resources"));
    }
}
