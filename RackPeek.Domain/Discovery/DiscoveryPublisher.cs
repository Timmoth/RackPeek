using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RackPeek.Domain.Api;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Sends a discovery document to a running RackPeek server's inventory API.
///     Always merges — discovery adds and updates what it finds, and must never be able
///     to remove what it did not.
/// </summary>
public sealed class DiscoveryPublisher : IDisposable {
    public const string ServerEnvironmentVariable = "RPK_SERVER";
    public const string ApiKeyEnvironmentVariable = "RPK_API_KEY";

    private static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;

    public DiscoveryPublisher(string serverUrl, string apiKey, HttpClient? httpClient = null) {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(serverUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public void Dispose() => _httpClient.Dispose();

    public static string? ResolveServer(string? explicitValue) =>
        Resolve(explicitValue, ServerEnvironmentVariable);

    public static string? ResolveApiKey(string? explicitValue) =>
        Resolve(explicitValue, ApiKeyEnvironmentVariable);

    /// <summary>An explicit value wins; a blank one falls back to the environment.</summary>
    public static string? Resolve(string? explicitValue, string environmentVariable) =>
        Coalesce(explicitValue, Environment.GetEnvironmentVariable(environmentVariable));

    public async Task<ImportYamlResponse> PublishAsync(
        string yaml,
        bool dryRun,
        CancellationToken cancellationToken = default) {
        var payload = JsonSerializer.Serialize(
            new ImportYamlRequest { Yaml = yaml, Mode = MergeMode.Merge, DryRun = dryRun },
            _jsonOptions);

        using var content = new StringContent(payload, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using HttpResponseMessage response =
            await _httpClient.PostAsync("api/inventory", content, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(Describe(response.StatusCode, body));

        return JsonSerializer.Deserialize<ImportYamlResponse>(body, _jsonOptions)
               ?? new ImportYamlResponse();
    }

    private static string Describe(System.Net.HttpStatusCode statusCode, string body) {
        var detail = ExtractError(body);

        return statusCode switch {
            System.Net.HttpStatusCode.Unauthorized =>
                "Rejected by the server (401). Check the API key matches RPK_API_KEY on the server.",
            System.Net.HttpStatusCode.ServiceUnavailable =>
                "The server has no API key configured (503). Set RPK_API_KEY on the RackPeek server.",
            _ => $"Upload failed ({(int)statusCode}). {detail}".TrimEnd()
        };
    }

    private static string ExtractError(string body) {
        try {
            using var document = JsonDocument.Parse(body);

            return document.RootElement.TryGetProperty("error", out JsonElement error)
                ? error.GetString() ?? string.Empty
                : body;
        }
        catch {
            return body;
        }
    }

    private static string? Coalesce(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
