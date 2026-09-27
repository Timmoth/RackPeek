using System.Net;
using System.Text;
using ModelContextProtocol.Client;
using RackPeek.Mcp.Tools;

namespace Tests.Mcp;

/// <summary>
///     /mcp sits behind the same X-Api-Key gate as /api/inventory: 503 until the
///     server has a key configured (MCP is off by default), 401 on a wrong key.
///     The raw-HTTP tests pin the status codes; the client-level tests prove the
///     gate actually stops an MCP session, not just a request.
/// </summary>
public class AuthTests {
    private const string _initializeBody =
        """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
        """;

    private static HttpRequestMessage InitializeRequest() {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") {
            Content = new StringContent(_initializeBody, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Accept", "application/json, text/event-stream");

        return request;
    }

    [Fact]
    public async Task Without_a_configured_key_the_endpoint_answers_503_and_stays_shut() {
        using var api = new McpFixture(extraConfig: new Dictionary<string, string?> {
            ["RPK_API_KEY"] = null
        });

        using HttpClient client = api.CreateHttpClient(apiKey: null);
        using HttpResponseMessage response = await client.SendAsync(InitializeRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task A_missing_key_is_rejected_with_401() {
        using var api = new McpFixture();

        using HttpClient client = api.CreateHttpClient(apiKey: null);
        using HttpResponseMessage response = await client.SendAsync(InitializeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_wrong_key_is_rejected_with_401() {
        using var api = new McpFixture();

        using HttpClient client = api.CreateHttpClient("not-the-key");
        using HttpResponseMessage response = await client.SendAsync(InitializeRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_mcp_client_without_the_key_cannot_even_finish_the_handshake() {
        using var api = new McpFixture();

        await Assert.ThrowsAnyAsync<Exception>(() => api.ConnectAsync(apiKey: null));
    }

    [Fact]
    public async Task The_right_key_opens_the_full_tool_surface() {
        using var api = new McpFixture();
        await using McpClient client = await api.ConnectAsync();

        ResourceList resources = await client.CallOkAsync<ResourceList>("list_resources");

        Assert.Equal(0, resources.Count);
    }
}
