using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources.Services.Networking;

namespace Tests.Discovery;

/// <summary>
///     The sweep's decisions, driven through a scripted probe: what counts as alive,
///     what IO happens for dead hosts, and that the concurrency cap actually caps.
///     The probe is the IO seam — everything above it is what these tests own.
/// </summary>
public class NetworkScannerTests {
    private static NetworkScanOptions Options(string cidr = "10.0.0.0/30", params int[] ports) =>
        new() {
            Cidr = Cidr.Parse(cidr),
            Ports = ports.Length > 0 ? ports : [22, 80],
            PingTimeout = TimeSpan.FromMilliseconds(5),
            PortTimeout = TimeSpan.FromMilliseconds(5)
        };

    [Fact]
    public async Task A_host_that_answers_nothing_is_not_reported() {
        var probe = new ScriptedProbe();

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, Options());

        Assert.Empty(hosts);
    }

    [Fact]
    public async Task A_ping_reply_alone_makes_a_host_alive_and_skips_its_port_probes() {
        var probe = new ScriptedProbe { PingReplies = ["10.0.0.1"] };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, Options());

        NetworkHostFact host = Assert.Single(hosts);
        Assert.Equal("10.0.0.1", host.Ip);
        Assert.True(host.AnsweredPing);
        // Liveness is already proven; knocking on ports would just be noise on the wire.
        Assert.DoesNotContain(probe.PortProbes, p => p.Ip == "10.0.0.1");
    }

    [Fact]
    public async Task A_host_that_drops_ping_but_serves_tcp_is_still_alive() {
        var probe = new ScriptedProbe { OpenPorts = [("10.0.0.2", 80)] };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, Options());

        NetworkHostFact host = Assert.Single(hosts);
        Assert.Equal("10.0.0.2", host.Ip);
        Assert.False(host.AnsweredPing);
        Assert.Equal([80], host.OpenPorts);
    }

    [Fact]
    public async Task Port_probing_stops_at_the_first_answer() {
        var probe = new ScriptedProbe { OpenPorts = [("10.0.0.2", 22), ("10.0.0.2", 80)] };

        await NetworkScanner.ScanAsync(probe, Options());

        // 22 answered, so 80 was never asked: the sweep proves liveness, not a port map.
        Assert.Equal([("10.0.0.2", 22)], probe.PortProbes.Where(p => p.Ip == "10.0.0.2"));
    }

    [Fact]
    public async Task The_arp_table_is_read_after_the_sweep_and_names_resolve_only_for_the_living() {
        var probe = new ScriptedProbe {
            PingReplies = ["10.0.0.1"],
            Arp = "? (10.0.0.1) at a4:91:b1:4e:3c:20 on en0 ifscope [ethernet]",
            Names = { ["10.0.0.1"] = "router.lan" }
        };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, Options());

        Assert.True(probe.ArpReadAfterSweep,
            "ARP must be read after the sweep — the sweep's own probes populate it.");
        Assert.Equal("a4:91:b1:4e:3c:20", hosts[0].Mac);
        Assert.Equal("router.lan", hosts[0].Hostname);
        Assert.Equal(["10.0.0.1"], probe.DnsLookups); // dead hosts get no PTR queries
    }

    [Fact]
    public async Task Results_come_back_in_address_order_whatever_order_probes_finished() {
        var probe = new ScriptedProbe { PingReplies = ["10.0.0.2", "10.0.0.1"] };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, Options());

        Assert.Equal(["10.0.0.1", "10.0.0.2"], hosts.Select(h => h.Ip));
    }

    [Fact]
    public async Task No_more_hosts_are_probed_at_once_than_the_options_allow() {
        var probe = new ScriptedProbe { PingDelay = TimeSpan.FromMilliseconds(20) };
        NetworkScanOptions options = Options("10.0.0.0/24") with { Concurrency = 4 };

        await NetworkScanner.ScanAsync(probe, options);

        Assert.True(probe.MaxInFlight <= 4,
            $"{probe.MaxInFlight} hosts were probed at once; the cap was 4.");
    }

    /// <summary>Scripted IO: answers what it is told to, records what was asked of it.</summary>
    private sealed class ScriptedProbe : INetworkProbe {
        private readonly Lock _lock = new();
        private int _inFlight;
        private bool _sweepDone;

        public List<string> PingReplies { get; init; } = [];
        public List<(string Ip, int Port)> OpenPorts { get; init; } = [];
        public string? Arp { get; init; }
        public Dictionary<string, string> Names { get; } = [];
        public TimeSpan PingDelay { get; init; } = TimeSpan.Zero;

        public List<(string Ip, int Port)> PortProbes { get; } = [];
        public List<string> DnsLookups { get; } = [];
        public int MaxInFlight { get; private set; }
        public bool ArpReadAfterSweep { get; private set; }

        public async Task<bool> PingAsync(string ip, TimeSpan timeout, CancellationToken cancellationToken = default) {
            lock (_lock) {
                _inFlight++;
                MaxInFlight = Math.Max(MaxInFlight, _inFlight);
            }

            try {
                if (PingDelay > TimeSpan.Zero)
                    await Task.Delay(PingDelay, cancellationToken);

                return PingReplies.Contains(ip);
            }
            finally {
                lock (_lock) {
                    _inFlight--;
                }
            }
        }

        public Task<bool> TryConnectAsync(
            string ip,
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken = default) {
            lock (_lock) {
                PortProbes.Add((ip, port));
            }

            return Task.FromResult(OpenPorts.Contains((ip, port)));
        }

        public Task<string?> ReadArpAsync(CancellationToken cancellationToken = default) {
            lock (_lock) {
                _sweepDone = true;
                ArpReadAfterSweep = _inFlight == 0;
            }

            return Task.FromResult(Arp);
        }

        public Task<string?> ReverseDnsAsync(string ip, CancellationToken cancellationToken = default) {
            lock (_lock) {
                if (!_sweepDone)
                    throw new InvalidOperationException("Reverse DNS ran before the sweep finished.");

                DnsLookups.Add(ip);
            }

            return Task.FromResult(Names.GetValueOrDefault(ip));
        }

        public Cidr? LocalSubnet() => null;
    }
}
