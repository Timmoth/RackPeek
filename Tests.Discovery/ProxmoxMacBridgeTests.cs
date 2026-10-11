using RackPeek.Domain.Api;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Proxmox's side of the MAC bridge: guest configs carry the NIC MACs Proxmox
///     assigned, a network scan sees exactly those MACs on the wire, and the resolver
///     lands both collectors on one card — the same contract the system collector has.
/// </summary>
public class ProxmoxMacBridgeTests {
    // -- parsing ------------------------------------------------------------------------

    [Theory]
    [InlineData("pve-qemu-config.json", "bc:24:11:12:34:56")] // virtio=BC:24:11:…
    [InlineData("pve-lxc-config.json", "bc:24:11:aa:bb:cc")] // hwaddr=BC:24:11:…
    [InlineData("pve-lxc-config-dhcp.json", "bc:24:11:dd:ee:ff")] // dhcp still has a MAC
    public void A_guests_config_yields_its_normalised_mac(string fixture, string expected) {
        ProxmoxGuestConfig config = ProxmoxResponseParser.ParseGuestConfig(Fixture.Read(fixture));

        Assert.Equal([expected], config.Macs);
    }

    [Theory]
    [InlineData("""{"data":{}}""")]
    [InlineData("""{"data":{"ostype":"l26","scsi0":"local-lvm:vm-1-disk-0,size=64G"}}""")]
    [InlineData("""{"data":{"net0":"bridge=vmbr0,firewall=1"}}""")] // a net line with no MAC
    [InlineData("""{"data":{"network":"virtio=BC:24:11:12:34:56"}}""")] // not a netN slot
    public void A_config_without_nic_macs_yields_none(string json) {
        ProxmoxGuestConfig config = ProxmoxResponseParser.ParseGuestConfig(json);

        Assert.Empty(config.Macs ?? []);
    }

    [Fact]
    public void Every_nic_of_a_multi_homed_guest_is_recorded() {
        ProxmoxGuestConfig config = ProxmoxResponseParser.ParseGuestConfig(
            """
            {"data":{
              "net0":"virtio=BC:24:11:12:34:56,bridge=vmbr0",
              "net1":"e1000=BC:24:11:99:88:77,bridge=vmbr1,tag=50"
            }}
            """);

        Assert.Equal(["bc:24:11:12:34:56", "bc:24:11:99:88:77"], config.Macs);
    }

    // -- mapping ------------------------------------------------------------------------

    [Fact]
    public void A_guest_card_carries_its_macs_label() {
        List<Resource> resources = ProxmoxResourceMapper.ToResources(
            "homelab",
            [new ProxmoxNode { Name = "pve01" }],
            [
                new ProxmoxGuest {
                    VmId = 104,
                    Node = "pve01",
                    Name = "docker-01",
                    Type = "vm",
                    Macs = ["bc:24:11:12:34:56"]
                }
            ]);

        SystemResource guest = resources.OfType<SystemResource>().Single(r => r.Name == "docker-01");
        Assert.Equal("bc:24:11:12:34:56", guest.Labels["macs"]);
    }

    // -- the bridge, end to end through the real server ----------------------------------

    [Fact]
    public async Task A_scanned_guest_and_its_proxmox_card_become_one() {
        // The sweep found the VM on the LAN first — by the very MAC Proxmox assigned it.
        var scanned = DiscoveryDocument.ToYaml(NetworkScanMapper.ToResources([
            new NetworkHostFact("192.168.1.178", "bc:24:11:12:34:56", null, true, [])
        ]));

        using var api = new DiscoveryApiFixture(scanned);

        // Now `rpk discover proxmox` reports the estate, including that guest.
        List<Resource> estate = ProxmoxResourceMapper.ToResources(
            "homelab",
            [new ProxmoxNode { Name = "pve01" }],
            [
                new ProxmoxGuest {
                    VmId = 104,
                    Node = "pve01",
                    Name = "docker-01",
                    Type = "vm",
                    Cores = 4,
                    Os = "Linux",
                    Macs = ["bc:24:11:12:34:56"]
                }
            ]);

        ImportYamlResponse response = await api.PublishAsync(DiscoveryDocument.ToYaml(estate));

        var stored = api.StoredYaml;

        // The guest landed on the scan's card: identity upgraded to the vmid-based id,
        // the scan's address kept, no duplicate for the same machine.
        Assert.DoesNotContain("rpk1:net:", stored);
        Assert.Contains("rpk1:pve:", stored);
        Assert.Contains("ip: 192.168.1.178", stored);
        Assert.Contains("os: Linux", stored);
        Assert.DoesNotContain(response.Added, name => name.StartsWith("docker-01"));
        Fixture.AssertConformsToSchema(stored);

        // And a rescan afterwards still lands on that same card via the MAC.
        ImportYamlResponse rescan = await api.PublishAsync(DiscoveryDocument.ToYaml(
            NetworkScanMapper.ToResources([
                new NetworkHostFact("192.168.1.179", "bc:24:11:12:34:56", null, true, [])
            ])));

        Assert.Empty(rescan.Added);
        Assert.Contains("rpk1:pve:", api.StoredYaml);
        Assert.Contains("ip: 192.168.1.179", api.StoredYaml);
    }
}
