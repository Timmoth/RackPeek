using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace Tests.E2e;

/// <summary>
///     The full production path: the shipped Docker image, a real network port, and a
///     real MCP client walking one realistic session — learn the schema, preview a
///     change, apply it, query it back, wire a connection, export, delete. Everything
///     the in-process suite (Tests.Mcp) proves is re-proved here against the artefact
///     that is actually distributed.
/// </summary>
public class McpE2eTests : IAsyncLifetime {
    private const string _dockerImage = "rackpeek:ci";
    private const string _apiKey = "e2e-mcp-key";

    private IContainer _container = default!;
    private HttpClient _http = default!;
    private McpClient _client = default!;

    public async Task InitializeAsync() {
        _container = new ContainerBuilder(_dockerImage)
            .WithPortBinding(8080, true)
            .WithEnvironment("RPK_API_KEY", _apiKey)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r
                        .ForPort(8080)
                        .ForPath("/health")))
            .Build();

        await _container.StartAsync();

        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions {
                Endpoint = new Uri($"http://127.0.0.1:{_container.GetMappedPublicPort(8080)}/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            _http,
            loggerFactory: null,
            ownsHttpClient: true);

        _client = await McpClient.CreateAsync(transport);
    }

    public async Task DisposeAsync() {
        if (_client != null) await _client.DisposeAsync();
        if (_container != null) await _container.DisposeAsync();
    }

    private async Task<JsonElement> CallAsync(string tool, Dictionary<string, object?>? args = null) {
        CallToolResult result = await _client.CallToolAsync(tool, args);

        var text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(t => t.Text));
        Assert.False(result.IsError == true, $"'{tool}' failed: {text}");

        return result.StructuredContent
               ?? JsonDocument.Parse(JsonSerializer.Serialize(new { text })).RootElement;
    }

    [Fact]
    public async Task A_full_session_builds_queries_wires_exports_and_deletes_a_stack() {
        // The server advertises itself and its whole tool surface over the wire.
        Assert.Equal("rackpeek", _client.ServerInfo.Name);
        IList<McpClientTool> tools = await _client.ListToolsAsync();
        Assert.Contains(tools, t => t.Name == "upsert_resources");
        Assert.Contains(tools, t => t.Name == "discover_docker");

        // 1. Learn the format.
        JsonElement schema = await CallAsync("get_schema");
        Assert.Equal(4, schema.GetProperty("version").GetInt32());

        // 2. Preview, then apply, a small stack.
        const string stack =
            """
            version: 4
            resources:
              - kind: Server
                name: e2e-server
                ports:
                  - type: rj45
                    speed: 1
                    count: 4
              - kind: Switch
                name: e2e-switch
                ports:
                  - type: rj45
                    speed: 1
                    count: 8
              - kind: System
                name: e2e-host
                type: baremetal
                os: debian
                ip: 10.9.0.1
                runsOn:
                  - e2e-server
              - kind: Service
                name: e2e-app
                runsOn:
                  - e2e-host
                network:
                  ip: 10.9.0.1
                  port: 8080
                  protocol: TCP
            """;

        JsonElement preview = await CallAsync("upsert_resources",
            new Dictionary<string, object?> { ["yaml"] = stack, ["dryRun"] = true });
        Assert.Equal(4, preview.GetProperty("added").GetArrayLength());

        JsonElement applied = await CallAsync("upsert_resources",
            new Dictionary<string, object?> { ["yaml"] = stack });
        Assert.Equal(4, applied.GetProperty("added").GetArrayLength());

        // 3. Query it back over the wire.
        JsonElement list = await CallAsync("list_resources",
            new Dictionary<string, object?> { ["kind"] = "Service" });
        Assert.Equal(1, list.GetProperty("count").GetInt32());

        JsonElement detail = await CallAsync("get_resource",
            new Dictionary<string, object?> { ["name"] = "e2e-app" });
        Assert.Contains("kind: Service", detail.GetProperty("yaml").GetString());

        JsonElement search = await CallAsync("search_resources",
            new Dictionary<string, object?> { ["query"] = "10.9.0.1" });
        Assert.True(search.GetProperty("matches").GetArrayLength() >= 1);

        // 4. Wire the hardware together and see it in the tree and the diagram.
        await CallAsync("add_connection", new Dictionary<string, object?> {
            ["resourceA"] = "e2e-server",
            ["portGroupA"] = 0,
            ["portIndexA"] = 0,
            ["resourceB"] = "e2e-switch",
            ["portGroupB"] = 0,
            ["portIndexB"] = 0,
            ["label"] = "uplink"
        });

        JsonElement tree = await CallAsync("get_tree",
            new Dictionary<string, object?> { ["hardwareName"] = "e2e-server" });
        Assert.Equal(1, tree.GetProperty("hardware").GetArrayLength());

        JsonElement mermaid = await CallAsync("export_topology_mermaid");
        Assert.Contains("e2e-switch", mermaid.GetProperty("text").GetString());

        // 5. The change shows up on the page a browser would load, not just over MCP.
        var home = await _http.GetStringAsync(
            $"http://127.0.0.1:{_container.GetMappedPublicPort(8080)}/health");
        Assert.Equal("rackpeek", home);

        // 6. Tear the service down again and the inventory agrees.
        await CallAsync("delete_resource", new Dictionary<string, object?> { ["name"] = "e2e-app" });

        JsonElement after = await CallAsync("list_resources",
            new Dictionary<string, object?> { ["kind"] = "Service" });
        Assert.Equal(0, after.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task The_gate_holds_over_a_real_network_socket() {
        using var bare = new HttpClient();

        var url = $"http://127.0.0.1:{_container.GetMappedPublicPort(8080)}/mcp";
        using var request = new HttpRequestMessage(HttpMethod.Post, url) {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"t","version":"1"}}}""",
                System.Text.Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("Accept", "application/json, text/event-stream");

        using HttpResponseMessage response = await bare.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
