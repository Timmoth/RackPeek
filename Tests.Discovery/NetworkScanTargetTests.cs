using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources.Services.Networking;

namespace Tests.Discovery;

/// <summary>
///     Which addresses a block actually sweeps. Getting the edges wrong either wastes
///     probes on the network/broadcast addresses or — worse — skips real hosts on the
///     point-to-point prefixes where every address is a host.
/// </summary>
public class NetworkScanTargetTests {
    private static List<string> Targets(string cidr) =>
        NetworkScanner.EnumerateTargets(Cidr.Parse(cidr)).ToList();

    [Fact]
    public void A_24_sweeps_the_254_host_addresses() {
        List<string> targets = Targets("192.168.1.0/24");

        Assert.Equal(254, targets.Count);
        Assert.Equal("192.168.1.1", targets.First());
        Assert.Equal("192.168.1.254", targets.Last());
        Assert.DoesNotContain("192.168.1.0", targets);
        Assert.DoesNotContain("192.168.1.255", targets);
    }

    [Fact]
    public void A_30_has_two_hosts_between_network_and_broadcast() =>
        Assert.Equal(["10.0.0.1", "10.0.0.2"], Targets("10.0.0.0/30"));

    [Fact]
    public void A_31_is_point_to_point_where_both_addresses_are_hosts() =>
        // RFC 3021: /31 has no network or broadcast address.
        Assert.Equal(["10.0.0.0", "10.0.0.1"], Targets("10.0.0.0/31"));

    [Fact]
    public void A_32_is_exactly_the_one_address() =>
        Assert.Equal(["127.0.0.1"], Targets("127.0.0.1/32"));

    [Fact]
    public void A_16_sweeps_the_full_65534_hosts() =>
        Assert.Equal(65_534, Targets("10.20.0.0/16").Count);

    [Fact]
    public void A_block_at_the_top_of_the_address_space_does_not_wrap() {
        List<string> targets = Targets("255.255.255.252/30");

        Assert.Equal(["255.255.255.253", "255.255.255.254"], targets);
    }

    [Fact]
    public void The_offered_ip_need_not_be_the_network_address() =>
        // People type their own address plus a prefix; Cidr.Parse masks it down.
        Assert.Equal(254, Targets("192.168.1.37/24").Count);
}
