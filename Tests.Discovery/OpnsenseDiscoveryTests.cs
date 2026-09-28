using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     A firewall's neighbour table in, System resources out.
///     <para>
///         This is the collector for everything a sweep can see but not identify. ARP is
///         link-local, so sweeping from one host yields a MAC for that host's own segment
///         and an address for everything else — and an address alone cannot survive a
///         DHCP re-lease or be matched to anything. The firewall routes every subnet, so
///         its table has the MAC, the name it handed out, and which leg it was seen on.
///     </para>
/// </summary>
public class OpnsenseDiscoveryTests {
    private static List<OpnsenseNeighbour> Neighbours() =>
        OpnsenseResponseParser.ParseArp(Fixture.Read("opnsense-arp.json"));

    private static List<Resource> Discover(bool includePublic = false) =>
        OpnsenseDiscovery.ToResources(Neighbours(), includePublic);

    [Fact]
    public void The_table_yields_one_neighbour_per_machine() {
        List<OpnsenseNeighbour> neighbours = Neighbours();

        // Of seven rows: one is the firewall's own address, one has aged out, one is the
        // broadcast address, and one is a second sighting of a host that answers on two
        // of the firewall's legs.
        Assert.Equal(3, neighbours.Count);
    }

    [Fact]
    // Every routed subnet contributes one, and they are all the same box — which is a
    // Firewall, not the handful of Systems this would otherwise invent.
    public void An_address_the_firewall_holds_itself_is_not_a_neighbour() =>
        Assert.DoesNotContain(Neighbours(), n => n.Mac == "00:1b:21:00:1a:02");

    [Fact]
    // It says where something used to be, which is not evidence that it is there now.
    public void An_entry_that_has_aged_out_is_not_a_neighbour() =>
        Assert.DoesNotContain(Neighbours(), n => n.Hostname == "gone-away");

    [Fact]
    public void The_broadcast_address_is_not_a_machine() =>
        Assert.DoesNotContain(Neighbours(), n => n.Mac.StartsWith("ff:", StringComparison.Ordinal));

    [Fact]
    // Its identity is the MAC, so two sightings must not become two machines.
    public void A_host_seen_on_two_legs_is_still_one_card() =>
        Assert.Single(Discover().OfType<SystemResource>(), s => s.Labels["mac"] == "1c:6a:1b:00:1a:03");

    [Fact]
    // It is not the user's infrastructure, and recording it would put a public address
    // into a file people commit.
    public void The_isps_equipment_on_the_wan_leg_is_left_out_by_default() =>
        Assert.DoesNotContain(Discover().OfType<SystemResource>(), s => s.Ip == "198.51.100.7");

    [Fact]
    // A fleet documented on public addresses is a real case; it just is not the default.
    public void Public_neighbours_can_be_asked_for() =>
        Assert.Contains(Discover(true).OfType<SystemResource>(), s => s.Ip == "198.51.100.7");

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.4.9")]
    [InlineData("172.31.255.254")]
    [InlineData("192.168.1.1")]
    [InlineData("100.64.0.1")] // carrier-grade NAT
    public void Private_and_carrier_ranges_count_as_ones_own_network(string ip) =>
        Assert.True(OpnsenseDiscovery.IsPrivate(ip));

    [Theory]
    [InlineData("198.51.100.7")]
    [InlineData("8.8.8.8")]
    [InlineData("172.15.0.1")] // just below the private block
    [InlineData("172.32.0.1")] // just above it
    [InlineData("100.63.0.1")] // just below the carrier block
    [InlineData("not-an-address")]
    public void Everything_else_does_not(string ip) =>
        Assert.False(OpnsenseDiscovery.IsPrivate(ip));

    [Fact]
    // The whole point: a sweep of another subnet can only call this host-<hash>.
    public void The_name_the_firewall_handed_out_becomes_the_card_name() =>
        Assert.Contains(Discover().OfType<SystemResource>(), s => s.Name == "forgejo");

    [Fact]
    public void A_neighbour_with_no_name_still_gets_a_stable_one() {
        List<Resource> cards = Discover(true);

        SystemResource wan = cards.OfType<SystemResource>().Single(s => s.Ip == "198.51.100.7");
        Assert.StartsWith("host-", wan.Name);
    }

    [Fact]
    public void A_card_carries_the_mac_the_vendor_and_the_leg_it_was_seen_on() {
        SystemResource card = Discover().OfType<SystemResource>().Single(s => s.Name == "forgejo");

        Assert.Equal("bc:24:11:00:1a:04", card.Labels["mac"]);
        Assert.Equal("Proxmox", card.Labels["vendor"]);
        Assert.Equal("HomeServices", card.Labels["segment"]);
        Assert.Equal("192.168.50.105", card.Ip);
    }

    [Fact]
    public void The_firewalls_own_vendor_lookup_fills_the_gaps_in_ours() {
        // Ours is a curated subset, so it answers "Proxmox" where the firewall says
        // "Proxmox Server Solutions GmbH" — but the firewall carries the whole registry
        // and answers for prefixes ours has never heard of.
        List<Resource> cards = Discover(true);

        SystemResource wan = cards.OfType<SystemResource>().Single(s => s.Ip == "198.51.100.7");
        Assert.Equal("Cisco Systems", wan.Labels["vendor"]);
    }

    [Fact]
    public void A_card_is_seeded_exactly_as_a_sweep_would_seed_it() {
        // The contract that makes the two collectors interchangeable: same evidence, same
        // identity, so a host the firewall knows and a host a sweep found are one card
        // whichever ran first — with no special case anywhere to say so.
        SystemResource fromFirewall = Discover().OfType<SystemResource>().Single(s => s.Name == "forgejo");

        SystemResource fromSweep = NetworkScanMapper.ToResources([
            new NetworkHostFact("192.168.50.105", "bc:24:11:00:1a:04", null, true, [])
        ]).OfType<SystemResource>().Single();

        Assert.Equal(fromSweep.DiscoveryId, fromFirewall.DiscoveryId);
    }

    [Fact]
    public void The_cards_stay_sparse() {
        // The firewall knows where a machine is and what its NIC is, never what runs on
        // it. A guess here would overwrite the real values on the next run of a collector
        // that does know.
        SystemResource card = Discover().OfType<SystemResource>().Single(s => s.Name == "forgejo");

        Assert.Null(card.Type);
        Assert.Null(card.Os);
        Assert.Null(card.Cores);
        Assert.Null(card.Ram);
    }

    [Fact]
    public void The_paginated_shape_reads_the_same_as_the_flat_one() {
        // The search wrapper returns {rows: [...]} while the direct call returns a bare
        // array; a caller should not have to know which endpoint answered.
        var rows = $$"""{"rows": {{Fixture.Read("opnsense-arp.json")}}, "total": 7}""";

        Assert.Equal(
            Neighbours().Select(n => n.Mac),
            OpnsenseResponseParser.ParseArp(rows).Select(n => n.Mac));
    }

    [Theory]
    [InlineData("""[{"mac":"bc:24:11:00:1a:04","ip":"192.168.50.105","permanent":"1"}]""")]
    [InlineData("""[{"mac":"bc:24:11:00:1a:04","ip":"192.168.50.105","permanent":1}]""")]
    [InlineData("""[{"mac":"bc:24:11:00:1a:04","ip":"192.168.50.105","permanent":true}]""")]
    public void A_flag_is_read_however_the_firmware_spells_it(string json) =>
        // OPNsense writes these as real booleans in some versions and as "1" in others.
        Assert.Empty(OpnsenseResponseParser.ParseArp(json));

    [Fact]
    public void An_empty_table_is_not_an_error() {
        Assert.Empty(OpnsenseResponseParser.ParseArp("[]"));
        Assert.Empty(OpnsenseResponseParser.ParseArp("""{"rows":[],"total":0}"""));
    }
}
