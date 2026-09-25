using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using RackPeek.Domain.Api;
using RackPeek.Domain.Discovery;
using RackPeek.Web;

namespace Tests.Discovery;

/// <summary>
///     A real RackPeek server backed by a temporary config file, driven through the
///     same <see cref="DiscoveryPublisher" /> the CLI uses. These are the end-to-end
///     tests: discovery YAML goes over HTTP into the inventory API and the assertions
///     are made against what actually lands on disk.
/// </summary>
public sealed class DiscoveryApiFixture : IDisposable {
    private const string _apiKey = "discovery-test-key";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _tempDir;

    /// <param name="initialConfig">
    ///     Contents to seed config.yaml with before the server first reads it — the way
    ///     to test how the server behaves against a file it did not write itself.
    /// </param>
    public DiscoveryApiFixture(string? initialConfig = null) {
        _tempDir = Path.Combine(Path.GetTempPath(), "rackpeek-discovery-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        if (initialConfig != null)
            File.WriteAllText(Path.Combine(_tempDir, "config.yaml"), initialConfig);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.UseSetting("RPK_YAML_DIR", _tempDir);
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?> {
                        ["RPK_YAML_DIR"] = _tempDir,
                        ["RPK_API_KEY"] = _apiKey
                    }));
            });
    }

    public string StoredYaml => File.ReadAllText(Path.Combine(_tempDir, "config.yaml"));

    public void Dispose() {
        try {
            _factory.Dispose();

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch {
            // Cleanup only; a leftover temp directory must never fail a test run.
        }
    }

    public async Task<ImportYamlResponse> PublishAsync(string yaml, bool dryRun = false) {
        HttpClient client = _factory.CreateClient();

        using var publisher = new DiscoveryPublisher(
            client.BaseAddress!.ToString(),
            _apiKey,
            client);

        return await publisher.PublishAsync(yaml, dryRun);
    }
}
