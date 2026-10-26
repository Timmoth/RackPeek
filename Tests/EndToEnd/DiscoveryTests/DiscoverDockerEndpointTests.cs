using System.Net;
using System.Net.Sockets;
using System.Text;
using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.DiscoveryTests;

/// <summary>
///     `rpk discover docker` against endpoints it cannot reach. Every case here fails
///     before any container is listed, so these tests never talk to a daemon.
/// </summary>
[Collection("Yaml CLI tests")]
public class DiscoverDockerEndpointTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<string> ExecuteAsync(params string[] args) =>
        await YamlCliTestHost.RunAsync(args, fs.Root, outputHelper, "config.yaml");

    [Theory]
    [InlineData("ssh://user@nas")]
    [InlineData("npipe:////./pipe/docker_engine")]
    public async Task an_endpoint_that_cannot_be_dialled_says_so_instead_of_crashing(string endpoint) {
        // `docker context` sets ssh:// for a remote host and npipe:// is the Windows
        // default, so both are ordinary values to find in DOCKER_HOST. They used to
        // reach HttpClient, which accepts the URI and throws NotSupportedException on
        // the first request — past the command's catch list, so the user got
        // "Unexpected error occurred" and a stack trace.
        var output = await ExecuteAsync("discover", "docker", "--docker-host", endpoint);

        Assert.DoesNotContain("Unexpected error", output);
        Assert.DoesNotContain("at RackPeek.", output);

        // And the message says what to do about it.
        Assert.Contains("ssh -L", output);
    }

    [Fact]
    public async Task remote_daemon_without_a_hostname_requires_an_explicit_host_and_emits_nothing() {
        await using var daemon = new FakeDockerDaemon("403 Forbidden", "{}");
        await daemon.StartAsync();
        var exitCode = -1;

        var output = await YamlCliTestHost.RunAsync(
            ["discover", "docker", "--docker-host", daemon.Endpoint],
            fs.Root,
            outputHelper,
            "config.yaml",
            code => exitCode = code);
        await daemon.Completion;

        Assert.Equal(1, exitCode);
        Assert.Contains("Could not determine the remote Docker host name", output);
        Assert.Contains("pass --host", output);
        Assert.DoesNotContain("kind: Service", output);
    }

    [Fact]
    public async Task remote_daemon_can_be_named_explicitly_when_info_is_blocked() {
        await using var daemon = new FakeDockerDaemon("403 Forbidden", "{}");
        await daemon.StartAsync();
        var exitCode = -1;

        var output = await YamlCliTestHost.RunAsync(
            ["discover", "docker", "--docker-host", daemon.Endpoint, "--host", "nas01"],
            fs.Root,
            outputHelper,
            "config.yaml",
            code => exitCode = code);
        await daemon.Completion;

        Assert.Equal(0, exitCode);
        Assert.Contains("name: app", output);
        Assert.Contains("nas01", output);
    }

    [Fact]
    public async Task remote_daemon_hostname_from_info_is_used_when_available() {
        await using var daemon = new FakeDockerDaemon("200 OK", """{"Name":"nas-from-engine","ID":"engine-1"}""");
        await daemon.StartAsync();
        var exitCode = -1;

        var output = await YamlCliTestHost.RunAsync(
            ["discover", "docker", "--docker-host", daemon.Endpoint],
            fs.Root,
            outputHelper,
            "config.yaml",
            code => exitCode = code);
        await daemon.Completion;

        Assert.Equal(0, exitCode);
        Assert.Contains("nas-from-engine", output);
    }

    [Fact]
    public async Task local_unix_socket_discovery_still_emits_the_host_and_service() {
        await using var daemon = new FakeUnixDockerDaemon(Path.Combine("/tmp", $"rpk-{Guid.NewGuid():N}.sock"));
        await daemon.StartAsync();
        var exitCode = -1;

        var output = await YamlCliTestHost.RunAsync(
            ["discover", "docker", "--docker-host", daemon.Endpoint],
            fs.Root,
            outputHelper,
            "config.yaml",
            code => exitCode = code);
        await daemon.Completion;

        Assert.Equal(0, exitCode);
        Assert.Contains("kind: System", output);
        Assert.Contains("kind: Service", output);
        Assert.Contains("runsOn:", output);
    }

    private sealed class FakeDockerDaemon(string infoStatus, string infoJson) : IAsyncDisposable {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        public string Endpoint => $"tcp://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";
        public Task Completion { get; private set; } = Task.CompletedTask;

        public Task StartAsync() {
            _listener.Start();
            Completion = ServeAsync();
            return Task.CompletedTask;
        }

        private async Task ServeAsync() {
            for (var i = 0; i < 2; i++) {
                using TcpClient connection = await _listener.AcceptTcpClientAsync();
                await using NetworkStream stream = connection.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                var request = await reader.ReadLineAsync();
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }

                (var status, var body) = request?.Contains("/info", StringComparison.Ordinal) == true
                    ? (infoStatus, infoJson)
                    : ("200 OK", """[{"Names":["/app"],"Image":"app:latest","Ports":[{"IP":"0.0.0.0","PublicPort":8080,"Type":"tcp"}]}]""");
                var bytes = Encoding.UTF8.GetBytes(body);
                var headers = Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 {status}\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(headers);
                await stream.WriteAsync(bytes);
            }
        }

        public ValueTask DisposeAsync() {
            _listener.Stop();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeUnixDockerDaemon(string socketPath) : IAsyncDisposable {
        private readonly Socket _listener = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        public string Endpoint => $"unix://{socketPath}";
        public Task Completion { get; private set; } = Task.CompletedTask;

        public Task StartAsync() {
            _listener.Bind(new UnixDomainSocketEndPoint(socketPath));
            _listener.Listen(2);
            Completion = ServeAsync();
            return Task.CompletedTask;
        }

        private async Task ServeAsync() {
            for (var i = 0; i < 1; i++) {
                using Socket connection = await _listener.AcceptAsync();
                await using var stream = new NetworkStream(connection, ownsSocket: false);
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                var request = await reader.ReadLineAsync();
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }

                var isInfo = request?.Contains("/info", StringComparison.Ordinal) == true;
                var body = isInfo
                    ? """{"Name":"local-daemon","ID":"local-engine"}"""
                    : """[{"Names":["/app"],"Image":"app:latest","Ports":[{"IP":"0.0.0.0","PublicPort":8080,"Type":"tcp"}]}]""";
                var bytes = Encoding.UTF8.GetBytes(body);
                var headers = Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(headers);
                await stream.WriteAsync(bytes);
            }
        }

        public ValueTask DisposeAsync() {
            _listener.Dispose();
            File.Delete(socketPath);
            return ValueTask.CompletedTask;
        }
    }
}
