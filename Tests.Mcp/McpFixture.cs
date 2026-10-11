using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using RackPeek.Web;

namespace Tests.Mcp;

/// <summary>
///     A real RackPeek server backed by a temporary config file, driven through a real
///     MCP client speaking streamable-HTTP JSON-RPC to /mcp. Every test in this project
///     goes end to end through this: if a behaviour is not observable here, an MCP
///     client cannot observe it either.
/// </summary>
public sealed class McpFixture : IDisposable {
    public const string ApiKey = "mcp-test-key";

    private readonly WebApplicationFactory<Program> _factory;

    /// <param name="initialConfig">
    ///     Contents to seed config.yaml with before the server first reads it.
    /// </param>
    /// <param name="extraConfig">
    ///     Extra configuration for the server — e.g. GIT_TOKEN to activate the git
    ///     tools, or RPK_API_KEY = null to model a server with no key configured.
    /// </param>
    public McpFixture(string? initialConfig = null, IDictionary<string, string?>? extraConfig = null) {
        TempDir = Path.Combine(Path.GetTempPath(), "rackpeek-mcp-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(TempDir);

        if (initialConfig != null)
            File.WriteAllText(ConfigPath, initialConfig);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.UseSetting("RPK_YAML_DIR", TempDir);

                // Settings read during startup (GIT_TOKEN wires services in BuildApp)
                // must go through UseSetting: the in-memory collection below lands too
                // late for service registration, though request-time reads see it fine.
                foreach ((var key, var value) in extraConfig ?? new Dictionary<string, string?>())
                    if (value != null)
                        builder.UseSetting(key, value);

                builder.ConfigureAppConfiguration((_, config) => {
                    var values = new Dictionary<string, string?> {
                        ["RPK_YAML_DIR"] = TempDir,
                        ["RPK_API_KEY"] = ApiKey
                    };

                    foreach ((var key, var value) in extraConfig ?? new Dictionary<string, string?>())
                        values[key] = value;

                    config.AddInMemoryCollection(values);
                });
            });
    }

    public string TempDir { get; }

    public string ConfigPath => Path.Combine(TempDir, "config.yaml");

    /// <summary>What is actually on disk — the ground truth every mutation test asserts on.</summary>
    public string StoredYaml => File.ReadAllText(ConfigPath);

    /// <summary>An HTTP client for driving /mcp (or anything else) below the MCP layer.</summary>
    public HttpClient CreateHttpClient(string? apiKey = ApiKey) {
        HttpClient client = _factory.CreateClient();

        if (apiKey != null)
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        return client;
    }

    /// <summary>A connected MCP client; the initialize handshake has already succeeded.</summary>
    public async Task<McpClient> ConnectAsync(string? apiKey = ApiKey) {
        HttpClient http = CreateHttpClient(apiKey);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions {
                Endpoint = new Uri(http.BaseAddress!, "mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            http,
            loggerFactory: null,
            ownsHttpClient: true);

        return await McpClient.CreateAsync(transport);
    }

    public void Dispose() {
        try {
            _factory.Dispose();

            if (Directory.Exists(TempDir))
                Directory.Delete(TempDir, true);
        }
        catch {
            // Cleanup only; a leftover temp directory must never fail a test run.
        }
    }
}
