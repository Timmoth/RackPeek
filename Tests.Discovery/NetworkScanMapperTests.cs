using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Swept hosts → the System cards RackPeek stores. The contract that matters most
///     is identity: MAC-seeded, format-independent, IP only as a last resort.
/// </summary>
public class NetworkScanMapperTests {
    private static NetworkHostFact Host(
        string ip = "192.168.1.20",
        string? mac = "dc:a6:32:0f:11:22",
        string? hostname = "nas01.lan") =>
        new(ip, mac, hostname, true, []);

    [Fact]
    public void A_host_becomes_a_system_card_with_ip_mac_and_its_dns_name() {
        List<Resource> resources = NetworkScanMapper.ToResources([Host()]);

        SystemResource system = Assert.IsType<SystemResource>(Assert.Single(resources));
        Assert.Equal("nas01", system.Name); // first label of nas01.lan
        Assert.Equal("System", system.Kind);
        Assert.Equal("192.168.1.20", system.Ip);
        // Deliberately sparse: anything a scan cannot see stays null so a rescan can
        // never overwrite what the user or an agent collector filled in.
        Assert.Null(system.Type);
        Assert.Null(system.Os);
        Assert.Null(system.Cores);
        Assert.Equal("dc:a6:32:0f:11:22", system.Labels["mac"]);
        Assert.StartsWith("rpk1:net:", system.DiscoveryId);
    }

    [Fact]
    public void Identity_rides_on_the_mac_so_a_dhcp_move_is_the_same_machine() {
        List<Resource> before = NetworkScanMapper.ToResources([Host(ip: "192.168.1.20")]);
        List<Resource> after = NetworkScanMapper.ToResources([Host(ip: "192.168.1.99")]);

        Assert.Equal(before[0].DiscoveryId, after[0].DiscoveryId);
    }

    [Fact]
    public void Without_a_mac_the_ip_seeds_the_identity_instead() {
        List<Resource> resources = NetworkScanMapper.ToResources([Host(mac: null)]);

        Assert.StartsWith("rpk1:net:", resources[0].DiscoveryId);
        Assert.False(Assert.IsType<SystemResource>(resources[0]).Labels.ContainsKey("mac"));

        // ...and it is a different identity than the MAC would have produced.
        Assert.NotEqual(
            NetworkScanMapper.ToResources([Host()])[0].DiscoveryId,
            resources[0].DiscoveryId);
    }

    [Fact]
    public void A_host_with_no_dns_name_gets_a_deterministic_one_from_its_id() {
        List<Resource> resources = NetworkScanMapper.ToResources([Host(hostname: null)]);

        Assert.StartsWith("host-", resources[0].Name);

        // Deterministic: the same machine names itself the same way on every run.
        Assert.Equal(resources[0].Name, NetworkScanMapper.ToResources([Host(hostname: null)])[0].Name);
    }

    [Fact]
    public void Two_hosts_answering_to_the_same_dns_name_stay_distinct() {
        // A lazy resolver that answers every PTR with the router's name must not
        // collapse the whole network into one card (the import rejects duplicates).
        List<Resource> resources = NetworkScanMapper.ToResources([
            Host(ip: "192.168.1.1", mac: "a4:91:b1:4e:3c:20", hostname: "router.lan"),
            Host(ip: "192.168.1.2", mac: "b0:00:00:00:00:02", hostname: "router.lan")
        ]);

        Assert.Equal(2, resources.Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal("router", resources[0].Name);
        Assert.StartsWith("router-", resources[1].Name);
    }

    [Fact]
    public void One_mac_answering_on_several_addresses_is_one_machine_with_one_card() {
        // Gateways answer on VIPs and aliases all the time: one MAC, many addresses —
        // still one box. Collapsing keeps the import happy (duplicate ids are rejected)
        // AND keeps identity independent of how many addresses answered this scan.
        List<Resource> resources = NetworkScanMapper.ToResources([
            Host(ip: "192.168.1.2", hostname: null), // the VIP, deliberately first
            Host(ip: "192.168.1.1", hostname: "gw.lan")
        ]);

        SystemResource card = Assert.IsType<SystemResource>(Assert.Single(resources));
        Assert.Equal("192.168.1.1", card.Ip); // the lowest address, deterministically
        Assert.Equal("gw", card.Name); // the one name anywhere in the group
        Assert.Equal("192.168.1.1,192.168.1.2", card.Labels["ips"]);
    }

    [Fact]
    public void A_vip_appearing_or_disappearing_never_moves_the_machines_identity() {
        // The regression that motivated the collapse: an id seeded on scan-local
        // address counts flips when a keepalived VIP fails over. MAC alone, always.
        List<Resource> alone = NetworkScanMapper.ToResources([
            Host(ip: "192.168.1.1", hostname: "gw.lan")
        ]);
        List<Resource> withVip = NetworkScanMapper.ToResources([
            Host(ip: "192.168.1.1", hostname: "gw.lan"),
            Host(ip: "192.168.1.2", hostname: null)
        ]);

        Assert.Equal(alone[0].DiscoveryId, Assert.Single(withVip).DiscoveryId);
    }

    [Fact]
    public void The_emitted_document_conforms_to_the_published_schema() {
        List<Resource> resources = NetworkScanMapper.ToResources([
            Host(),
            Host(ip: "192.168.1.30", mac: null, hostname: null)
        ]);

        Fixture.AssertConformsToSchema(DiscoveryDocument.ToYaml(resources));
    }
}
