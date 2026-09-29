using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;

namespace RackPeek.Domain.Discovery;

/// <summary>Reads an OPNsense firewall's API. The IO half of firewall discovery.</summary>
public interface IOpnsenseClient {
    /// <summary>Where this client is pointed, for error messages.</summary>
    string Endpoint { get; }

    /// <summary>
    ///     Every neighbour the firewall currently has an ARP entry for, across every
    ///     subnet it routes.
    /// </summary>
    Task<IReadOnlyList<OpnsenseNeighbour>> GetNeighboursAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Talks to the OPNsense API with a key and secret, which OPNsense issues per user
///     and sends as HTTP basic credentials. A key can be given a read-only role and
///     revoked on its own, so it is used rather than a login.
/// </summary>
public sealed class OpnsenseApiClient : IOpnsenseClient, IDisposable {
    public const string KeyEnvironmentVariable = "RPK_OPN_KEY";
    public const string SecretEnvironmentVariable = "RPK_OPN_SECRET";

    /// <summary>
    ///     The ARP endpoint, newest spelling first. OPNsense 25.7 renamed its API actions
    ///     from camelCase to snake_case and kept the old names only for a while — an
    ///     integration pinned to either one breaks on half the installations out there.
    ///     Asking for each in turn costs one extra request against older firmware and
    ///     nothing against new.
    /// </summary>
    private static readonly string[] _arpPaths = [
        "api/diagnostics/interface/get_arp",
        "api/diagnostics/interface/getArp"
    ];

    private readonly HttpClient _httpClient;

    /// <param name="allowUntrustedCertificate">
    ///     OPNsense ships with a self-signed certificate and most installations keep it.
    ///     Opt-in all the same.
    /// </param>
    public OpnsenseApiClient(
        string host,
        string key,
        string secret,
        bool allowUntrustedCertificate = false,
        HttpClient? httpClient = null) {
        Endpoint = Normalise(host);

        _httpClient = httpClient ?? new HttpClient(Handler(allowUntrustedCertificate));
        _httpClient.BaseAddress = new Uri(Endpoint + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}")));
    }

    public string Endpoint { get; }

    public void Dispose() => _httpClient.Dispose();

    public async Task<IReadOnlyList<OpnsenseNeighbour>> GetNeighboursAsync(
        CancellationToken cancellationToken = default) {
        HttpRequestException? last = null;

        foreach (var path in _arpPaths) {
            try {
                return OpnsenseResponseParser.ParseArp(await GetAsync(path, cancellationToken));
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound) {
                // Wrong spelling for this firmware; try the other.
                last = ex;
            }
        }

        throw last ?? new HttpRequestException("The firewall has no ARP endpoint this understands.");
    }

    /// <summary>
    ///     A bare name gets https, matching how the web UI is reached. The path is
    ///     trimmed so callers may paste a URL straight out of the browser.
    /// </summary>
    public static string Normalise(string host) {
        var trimmed = host.Trim().TrimEnd('/');

        if (!trimmed.Contains("://", StringComparison.Ordinal))
            trimmed = "https://" + trimmed;

        var uri = new Uri(trimmed);

        return uri.GetLeftPart(UriPartial.Authority);
    }

    private async Task<string> GetAsync(string path, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await _httpClient.GetAsync(path, cancellationToken);

        // A key without the right privilege is the common setup mistake, and OPNsense
        // answers it with a redirect to the login page rather than a 401.
        if (response.StatusCode is System.Net.HttpStatusCode.Found
            or System.Net.HttpStatusCode.MovedPermanently
            or System.Net.HttpStatusCode.Unauthorized
            or System.Net.HttpStatusCode.Forbidden)
            throw new HttpRequestException(
                $"The firewall refused the API key ({(int)response.StatusCode}). Check the key and secret, "
                + "and that its user holds the Diagnostics: ARP Table privilege.",
                null,
                response.StatusCode);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static HttpClientHandler Handler(bool allowUntrustedCertificate) {
        var handler = new HttpClientHandler();

        if (allowUntrustedCertificate)
            handler.ServerCertificateCustomValidationCallback =
                (_, _, _, _) => true;

        return handler;
    }
}
