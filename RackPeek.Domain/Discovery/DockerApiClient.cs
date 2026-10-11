using System.Net.Sockets;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Talks to the Docker Engine API over either a unix socket or TCP. Podman's socket
///     speaks the same API, so <c>--docker-host unix:///run/user/1000/podman/podman.sock</c>
///     works without anything extra.
/// </summary>
public sealed class DockerApiClient : IDockerClient, IDisposable {
    public const string DefaultSocketPath = "/var/run/docker.sock";

    private readonly HttpClient _httpClient;

    public DockerApiClient(string? dockerHost = null) {
        var host = string.IsNullOrWhiteSpace(dockerHost)
            ? Environment.GetEnvironmentVariable("DOCKER_HOST")
            : dockerHost;

        (_httpClient, Endpoint) = Create(host);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public string Endpoint { get; }

    /// <summary>
    ///     Whether the endpoint is a socket on this machine. Over TCP the engine is some
    ///     other machine, so facts probed locally must not be attributed to it.
    /// </summary>
    public bool IsLocal => Endpoint.StartsWith("unix://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     The host part of a TCP endpoint — where the containers actually live — or null
    ///     for a local socket. A name or an address, exactly as the user dialled it.
    /// </summary>
    public string? RemoteHost => IsLocal
        ? null
        : new Uri(Endpoint.Replace("tcp://", "http://", StringComparison.OrdinalIgnoreCase)).DnsSafeHost;

    public void Dispose() => _httpClient.Dispose();

    public async Task<IReadOnlyList<DockerContainer>> ListContainersAsync(
        CancellationToken cancellationToken = default) {
        using HttpResponseMessage response =
            await _httpClient.GetAsync("/containers/json", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        return DockerContainerParser.Parse(json);
    }

    public async Task<DockerEngineInfo?> GetInfoAsync(CancellationToken cancellationToken = default) {
        try {
            using HttpResponseMessage response = await _httpClient.GetAsync("/info", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            return DockerEngineInfoParser.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (Exception ex) when (
            ex is HttpRequestException or IOException or TimeoutException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)) {
            // /info being unreadable never fails discovery; the caller degrades instead.
            return null;
        }
    }

    /// <summary>
    ///     The address the user dialled, as IPv4: taken verbatim when it is a literal,
    ///     resolved once when it is a name. The inventory schema holds IPv4 only, so an
    ///     IPv6-only endpoint yields null and the caller falls back.
    /// </summary>
    public static async Task<string?> ResolveIpv4Async(string host, CancellationToken cancellationToken = default) {
        if (System.Net.IPAddress.TryParse(host, out System.Net.IPAddress? literal))
            return literal.AddressFamily == AddressFamily.InterNetwork ? host : null;

        try {
            System.Net.IPAddress[] addresses = await System.Net.Dns.GetHostAddressesAsync(host, cancellationToken);

            return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException or PlatformNotSupportedException) {
            // Unresolvable, malformed, or no DNS on this platform (the browser console):
            // the address is a nicety, never worth failing discovery over.
            return null;
        }
    }

    private static (HttpClient Client, string Endpoint) Create(string? dockerHost) {
        if (string.IsNullOrWhiteSpace(dockerHost))
            return (UnixSocketClient(DefaultSocketPath), $"unix://{DefaultSocketPath}");

        if (dockerHost.StartsWith("unix://", StringComparison.OrdinalIgnoreCase)) {
            var path = dockerHost["unix://".Length..];

            return (UnixSocketClient(path), dockerHost);
        }

        // Anything else is rejected here rather than at send time. HttpClient accepts an
        // ssh:// or npipe:// URI happily and only throws NotSupportedException on the
        // first request — which is not in the caller's catch list, so a DOCKER_HOST that
        // `docker context` set up quite normally crashed with a stack trace instead of
        // saying what was wrong.
        if (!StartsWithScheme(dockerHost, "tcp://")
            && !StartsWithScheme(dockerHost, "http://")
            && !StartsWithScheme(dockerHost, "https://"))
            // A UriFormatException on purpose: both front ends already turn that into
            // "not a usable Docker endpoint", so there is one phrasing for a bad endpoint
            // rather than two.
            throw new UriFormatException(
                "Use a unix socket (unix:///var/run/docker.sock) or a TCP endpoint "
                + "(tcp://host:2375). For an ssh:// context, forward the socket first — "
                + "ssh -L 2375:/var/run/docker.sock user@host — and point --docker-host at that.");

        // tcp:// is the scheme people have in DOCKER_HOST, but it is plain HTTP on the wire.
        var uri = new Uri(dockerHost.Replace("tcp://", "http://", StringComparison.OrdinalIgnoreCase));

        return (new HttpClient { BaseAddress = uri }, dockerHost);
    }

    private static bool StartsWithScheme(string value, string scheme) =>
        value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase);

    private static HttpClient UnixSocketClient(string socketPath) {
        var handler = new SocketsHttpHandler {
            ConnectCallback = async (_, cancellationToken) => {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

                try {
                    await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken);

                    return new NetworkStream(socket, true);
                }
                catch {
                    socket.Dispose();
                    throw;
                }
            }
        };

        // The host part is ignored for a unix socket but HttpClient still requires one.
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
    }
}
