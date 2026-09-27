using System.Net;
using System.Net.Sockets;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources.Services.Networking;

namespace Tests.Discovery;

/// <summary>
///     The real probe against loopback — the one network every CI runner has and the
///     tests are allowed to touch. TCP carries these tests on purpose: ICMP needs
///     privileges some runners lack, and the scanner's whole point is that liveness
///     never depends on ping alone.
/// </summary>
public class NetworkProbeLoopbackTests {
    [Fact]
    public async Task A_listening_port_answers_a_connect_probe() {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var probe = new NetworkProbe();

        Assert.True(await probe.TryConnectAsync("127.0.0.1", port, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task A_closed_port_says_no_instead_of_throwing() {
        // Bind-then-close guarantees the port exists and nothing is listening on it.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var probe = new NetworkProbe();

        Assert.False(await probe.TryConnectAsync("127.0.0.1", port, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task An_unroutable_address_gives_up_within_the_timeout_budget() {
        var probe = new NetworkProbe();
        DateTime started = DateTime.UtcNow;

        // TEST-NET-1 (RFC 5737) is never routed; the connect must die on OUR timer.
        var open = await probe.TryConnectAsync("192.0.2.1", 9, TimeSpan.FromMilliseconds(250));

        Assert.False(open);
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(5),
            "The connect ignored the timeout and sat on the OS default instead.");
    }

    [Fact]
    public async Task The_whole_scan_pipeline_finds_a_real_listener_on_loopback() {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(
            new NetworkProbe(),
            new NetworkScanOptions {
                Cidr = Cidr.Parse("127.0.0.1/32"),
                Ports = [port],
                // Loopback ping may be privilege-blocked on the runner; the open port
                // must carry the verdict alone, so keep the ping window tiny.
                PingTimeout = TimeSpan.FromMilliseconds(50),
                PortTimeout = TimeSpan.FromSeconds(2)
            });

        NetworkHostFact host = Assert.Single(hosts);
        Assert.Equal("127.0.0.1", host.Ip);
        Assert.True(host.AnsweredPing || host.OpenPorts.Contains(port));
    }

    [Fact]
    public async Task Scanning_a_dead_block_finds_nothing_and_finishes_quickly() {
        // Loopback cannot play the dead host: on Linux the whole 127/8 answers ping.
        // TEST-NET-1 (RFC 5737) is reserved and never routed, on every platform.
        DateTime started = DateTime.UtcNow;

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(
            new NetworkProbe(),
            new NetworkScanOptions {
                Cidr = Cidr.Parse("192.0.2.0/30"),
                Ports = [9],
                PingTimeout = TimeSpan.FromMilliseconds(50),
                PortTimeout = TimeSpan.FromMilliseconds(250)
            });

        Assert.Empty(hosts);
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(10));
    }
}
