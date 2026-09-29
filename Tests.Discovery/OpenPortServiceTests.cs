using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     An open port is a service, not a note on the host.
///     <para>
///         It used to land in an "open-ports" label — a comma-separated string that
///         nothing could link to, filter on, or hang a note from. RackPeek already has a
///         resource for "a thing listening on an address and a port", so a sweep that
///         finds 22 open on nebula should produce a Service called nebula-ssh running on
///         nebula, exactly as if someone had written it in by hand.
///     </para>
/// </summary>
public class OpenPortServiceTests {
    private static List<Resource> Scan(string ip, string? mac, params int[] ports) =>
        NetworkScanMapper.ToResources([
            new NetworkHostFact(ip, mac, "nebula", true, ports)
        ]);

    private static List<Service> Services(params int[] ports) =>
        Scan("192.0.2.20", "bc:24:11:00:2a:01", ports).OfType<Service>().ToList();

    [Fact]
    public void An_open_port_is_no_longer_a_label() {
        SystemResource host = Scan("192.0.2.20", "bc:24:11:00:2a:01", 22, 80)
            .OfType<SystemResource>()
            .Single();

        Assert.False(host.Labels.ContainsKey("open-ports"));
    }

    [Fact]
    public void Each_open_port_becomes_a_service_on_the_host() {
        List<Service> services = Services(22, 80);

        Assert.Equal(2, services.Count);
        Assert.All(services, s => Assert.Equal(["nebula"], s.RunsOn));
    }

    [Fact]
    public void A_service_is_named_for_the_host_and_what_the_port_serves() {
        Service ssh = Assert.Single(Services(22));

        Assert.Equal("nebula-ssh", ssh.Name);
    }

    [Theory]
    [InlineData(443, "nebula-https")]
    [InlineData(445, "nebula-smb")]
    [InlineData(1883, "nebula-mqtt")]
    [InlineData(8006, "nebula-proxmox")]
    [InlineData(32400, "nebula-plex")]
    public void Well_known_ports_are_named_by_what_they_serve(int port, string expected) =>
        Assert.Equal(expected, Assert.Single(Services(port)).Name);

    [Fact]
    // Better an honest tcp-9987 than a guess: the number is the only fact available.
    public void An_unrecognised_port_keeps_its_number() =>
        Assert.Equal("nebula-tcp-9987", Assert.Single(Services(9987)).Name);

    [Fact]
    public void A_service_that_named_itself_beats_what_its_port_implies() {
        // A port is a convention and an answer is evidence. Home Assistant on 8123 is
        // the convention; a page that says "Forgejo" is the machine telling you.
        List<Resource> resources = NetworkScanMapper.ToResources([
            new NetworkHostFact("192.0.2.21", "bc:24:11:00:2a:02", "nebula", true, [8123]) {
                Services = [new ServiceIdentity("Forgejo", IdentitySource.Http, 8123)]
            }
        ]);

        Assert.Equal("forgejo", Assert.Single(resources.OfType<Service>()).Name);
    }

    [Fact]
    public void A_service_records_where_it_is_listening() {
        Service ssh = Assert.Single(Services(22));

        Assert.Equal("192.0.2.20", ssh.Network?.Ip);
        Assert.Equal(22, ssh.Network?.Port);
        Assert.Equal("TCP", ssh.Network?.Protocol);
    }

    [Fact]
    public void A_services_identity_is_its_hosts_identity_and_the_port() {
        // Stable across rescans, and distinct per port, so a rescan updates the same two
        // cards rather than inventing a pair every time.
        var first = Services(22, 80).Select(s => s.DiscoveryId).ToList();
        var second = Services(22, 80).Select(s => s.DiscoveryId).ToList();

        Assert.Equal(first, second);
        Assert.Equal(2, first.Distinct().Count());
    }

    [Fact]
    public void Two_hosts_running_the_same_thing_get_distinct_cards() {
        // Naming after the host is what keeps these apart — "ssh" alone would collide on
        // every machine in the rack.
        List<Resource> resources = NetworkScanMapper.ToResources([
            new NetworkHostFact("192.0.2.30", "bc:24:11:00:2a:03", "nebula", true, [22]),
            new NetworkHostFact("192.0.2.31", "bc:24:11:00:2a:04", "orion", true, [22])
        ]);

        Assert.Equal(
            ["nebula-ssh", "orion-ssh"],
            resources.OfType<Service>().Select(s => s.Name).Order());
    }

    [Fact]
    public void A_host_with_nothing_listening_yields_no_services() =>
        Assert.Empty(Scan("192.0.2.40", "bc:24:11:00:2a:05").OfType<Service>());
}
