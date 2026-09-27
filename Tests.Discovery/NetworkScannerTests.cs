using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources.Services.Networking;

namespace Tests.Discovery;

/// <summary>
///     The sweep's decisions, driven through a scripted probe: what counts as alive,
///     what IO happens for dead hosts, and that the concurrency cap actually caps.
///     The probe is the IO seam — everything above it is what these tests own.
/// </summary>
public class NetworkScannerTests {
    // Identification is off unless a test asks for it, so the liveness tests keep
    // measuring only liveness.
    private static NetworkScanOptions Options(string cidr = "10.0.0.0/30", params int[] ports) =>
        new() {
            Cidr = Cidr.Parse(cidr),
            Ports = ports.Length > 0 ? ports : [22, 80],
            PingTimeout = TimeSpan.FromMilliseconds(5),
            PortTimeout = TimeSpan.FromMilliseconds(5),
            IdentifyServices = false
        };

    private static NetworkScanOptions IdentifyingOptions(string cidr = "10.0.0.0/30") =>
        Options(cidr) with {
            IdentifyServices = true,
            IdentifyTimeout = TimeSpan.FromMilliseconds(5)
        };

    [Fact]
    public async Task The_port_list_the_liveness_check_cut_short_is_finished_for_the_living() {
        var probe = new ScriptedProbe { OpenPorts = [("10.0.0.2", 22), ("10.0.0.2", 80)] };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(
            probe,
            IdentifyingOptions() with { Ports = [22, 80] });

        // Liveness stopped at 22; the interrogation pass goes back for the rest, so the
        // card records what the host actually serves rather than the first thing tried.
        Assert.Equal([22, 80], Assert.Single(hosts).OpenPorts);
    }

    [Fact]
    public async Task A_host_that_answered_ping_still_gets_its_ports_inventoried() {
        // Liveness skips port probing entirely once ping answers, which used to leave
        // every pingable host with no port evidence at all.
        var probe = new ScriptedProbe {
            PingReplies = ["10.0.0.1"],
            OpenPorts = [("10.0.0.1", 80)]
        };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(
            probe,
            IdentifyingOptions() with { Ports = [22, 80] });

        Assert.Equal([80], Assert.Single(hosts).OpenPorts);
    }

    [Fact]
    public async Task A_dead_host_is_never_interrogated() {
        var probe = new ScriptedProbe();

        await NetworkScanner.ScanAsync(probe, IdentifyingOptions() with { Ports = [22, 80] });

        Assert.Empty(probe.IdentityProbes);
    }

    [Fact]
    public async Task Only_living_hosts_are_asked_what_they_are() {
        // 443 has to be open for it to be asked: only ports the sweep found listening
        // are interrogated, so a closed port costs no handshake.
        var probe = new ScriptedProbe {
            PingReplies = ["10.0.0.1"],
            OpenPorts = [("10.0.0.1", 443)],
            TlsSubjects = { [("10.0.0.1", 443)] = "CN=router.lan" }
        };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, IdentifyingOptions());

        Assert.Equal("router.lan", Assert.Single(hosts).Identity?.Name);
        // 10.0.0.2 answered nothing, so it was never asked.
        Assert.DoesNotContain(probe.IdentityProbes, p => p.Ip == "10.0.0.2");
    }

    [Fact]
    public async Task A_certificate_names_the_host_even_when_other_ports_also_answer() {
        var probe = new ScriptedProbe {
            PingReplies = ["10.0.0.1"],
            OpenPorts = [("10.0.0.1", 443), ("10.0.0.1", 22)],
            TlsSubjects = { [("10.0.0.1", 443)] = "CN=pve-node-01.example.com" },
            Banners = { [("10.0.0.1", 22)] = "SSH-2.0-OpenSSH_9.6" }
        };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, IdentifyingOptions());

        NetworkHostFact host = Assert.Single(hosts);
        // Every open port is asked — an SSH greeting is still worth recording — but a
        // certificate is the only answer that is a claim about the machine itself.
        Assert.Equal("pve-node-01.example.com", host.Identity?.Name);
        Assert.Equal(IdentitySource.TlsCertificate, host.Identity?.Source);
    }

    [Fact]
    public async Task A_page_title_outranks_an_ssh_greeting() {
        var probe = new ScriptedProbe {
            PingReplies = ["10.0.0.1"],
            OpenPorts = [("10.0.0.1", 22), ("10.0.0.1", 80)],
            Banners = { [("10.0.0.1", 22)] = "SSH-2.0-dropbear" },
            HttpHeads = { [("10.0.0.1", 80)] = "HTTP/1.0 200 OK\r\n\r\n<title>Home Assistant</title>" }
        };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, IdentifyingOptions());

        // Every Linux box answers SSH with the same daemon name; the title says which
        // box this actually is, so SSH is asked last.
        Assert.Equal("Home Assistant", Assert.Single(hosts).Identity?.Name);
    }

    [Fact]
    public async Task An_open_port_the_sweep_found_is_also_asked() {
        // The liveness sweep already paid to learn 8123 was open. A service on a port
        // nobody curated is exactly the one worth asking.
        var probe = new ScriptedProbe {
            OpenPorts = [("10.0.0.2", 8123)],
            HttpHeads = { [("10.0.0.2", 8123)] = "HTTP/1.0 200 OK\r\n\r\n<title>Home Assistant</title>" }
        };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(
            probe,
            IdentifyingOptions() with { Ports = [8123] });

        NetworkHostFact host = Assert.Single(hosts);
        Assert.Equal("Home Assistant", host.Identity?.Name);
        Assert.Equal(8123, host.Identity?.Port);
    }

    [Fact]
    public async Task A_host_that_says_nothing_is_still_reported() {
        var probe = new ScriptedProbe { PingReplies = ["10.0.0.1"] };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, IdentifyingOptions());

        NetworkHostFact host = Assert.Single(hosts);
        Assert.Null(host.Identity);
        Assert.Equal("10.0.0.1", host.Ip);
    }

    [Fact]
    public async Task Turning_identification_off_asks_nothing() {
        var probe = new ScriptedProbe {
            PingReplies = ["10.0.0.1"],
            TlsSubjects = { [("10.0.0.1", 443)] = "CN=router.lan" }
        };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, Options());

        Assert.Null(Assert.Single(hosts).Identity);
        Assert.Empty(probe.IdentityProbes);
    }

    [Fact]
    public async Task A_hosts_vendor_comes_from_its_arp_mac() {
        var probe = new ScriptedProbe {
            PingReplies = ["10.0.0.1"],
            Arp = "? (10.0.0.1) at bc:24:11:00:1a:01 on en0 ifscope [ethernet]"
        };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, Options());

        Assert.Equal("Proxmox", Assert.Single(hosts).Vendor);
    }

    [Fact]
    public async Task A_host_with_no_arp_entry_has_no_vendor() {
        var probe = new ScriptedProbe { PingReplies = ["10.0.0.1"] };

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, Options());

        Assert.Null(Assert.Single(hosts).Vendor);
    }

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

    [Fact]
    public async Task A_block_wider_than_the_cap_is_refused_wherever_it_came_from() {
        // The floor lives in the scanner, not a front end: an auto-detected VPN /10
        // must hit the same wall a typed --cidr does.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            NetworkScanner.ScanAsync(new ScriptedProbe(), Options("10.0.0.0/8")));
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

        public Dictionary<(string Ip, int Port), string> TlsSubjects { get; } = [];
        public Dictionary<(string Ip, int Port), string> Banners { get; } = [];
        public Dictionary<(string Ip, int Port), string> HttpHeads { get; } = [];

        public List<(string Ip, int Port)> PortProbes { get; } = [];
        public List<string> DnsLookups { get; } = [];
        public List<(string Ip, int Port, string Kind)> IdentityProbes { get; } = [];
        public bool IsSupported => true;
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

        public Task<string?> ReverseDnsAsync(
            string ip,
            TimeSpan timeout,
            CancellationToken cancellationToken = default) {
            lock (_lock) {
                if (!_sweepDone)
                    throw new InvalidOperationException("Reverse DNS ran before the sweep finished.");

                DnsLookups.Add(ip);
            }

            return Task.FromResult(Names.GetValueOrDefault(ip));
        }

        public Task<string?> ReadTlsSubjectAsync(
            string ip,
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken = default) {
            lock (_lock) {
                IdentityProbes.Add((ip, port, "tls"));
            }

            return Task.FromResult(TlsSubjects.GetValueOrDefault((ip, port)));
        }

        public Task<string?> ReadTcpBannerAsync(
            string ip,
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken = default) {
            lock (_lock) {
                IdentityProbes.Add((ip, port, "banner"));
            }

            return Task.FromResult(Banners.GetValueOrDefault((ip, port)));
        }

        public Task<string?> ReadHttpHeadAsync(
            string ip,
            int port,
            bool tls,
            TimeSpan timeout,
            CancellationToken cancellationToken = default) {
            lock (_lock) {
                IdentityProbes.Add((ip, port, "http"));
            }

            return Task.FromResult(HttpHeads.GetValueOrDefault((ip, port)));
        }

        public Cidr? LocalSubnet() => null;
    }
}
