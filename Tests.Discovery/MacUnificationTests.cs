using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     The MAC bridge between collectors: an agent card carries the machine's MACs
///     (a "macs" label), a scan card carries the one it was found by (a "mac" label),
///     and a shared MAC means the same box — so both collectors land on one card,
///     whichever arrived first. These pin the resolver's side of that contract.
/// </summary>
public class MacUnificationTests {
    private const string _mac = "dc:a6:32:0f:11:22";

    private static SystemResource ScanCard(string name = "host-595109fb", string mac = _mac) => new() {
        Kind = SystemResource.KindLabel,
        Name = name,
        DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, mac),
        Ip = "192.168.1.20",
        Labels = { ["mac"] = mac }
    };

    private static SystemResource AgentCard(
        string name = "nas01",
        string machineId = "machine-a",
        string macs = _mac) => new() {
            Kind = SystemResource.KindLabel,
            Name = name,
            DiscoveryId = DiscoveryId.Create(DiscoveryId.SystemScheme, machineId),
            Type = "baremetal",
            Os = "Debian",
            Cores = 12,
            Labels = { ["macs"] = macs }
        };

    [Fact]
    public void An_agent_claims_the_scan_card_and_upgrades_its_identity() {
        // Scan ran first; now `rpk discover system` reports the same box.
        List<Resource> existing = [ScanCard()];
        List<Resource> incoming = [AgentCard()];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        // The stored card's name wins (names are user-owned), and the agent's id
        // survives so the merge upgrades the card to the stronger identity.
        Assert.Equal("host-595109fb", incoming[0].Name);
        Assert.StartsWith("rpk1:sys:", incoming[0].DiscoveryId);
    }

    [Fact]
    public void A_scan_enriches_the_agents_card_without_touching_its_identity() {
        // Agent ran first; now a sweep sees the same box from outside.
        List<Resource> existing = [AgentCard()];
        List<Resource> incoming = [ScanCard()];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("nas01", incoming[0].Name);
        // The weak scan id is dropped so the merge cannot downgrade the sys id.
        Assert.Null(incoming[0].DiscoveryId);
    }

    [Fact]
    public void The_macs_are_matched_however_each_side_spells_them() {
        // The agent records padded lowercase; suppose a stored label was hand-edited
        // to Windows-style dashes — normalisation makes them the same machine anyway.
        SystemResource stored = ScanCard();
        stored.Labels["mac"] = "DC-A6-32-0F-11-22";
        List<Resource> existing = [stored];

        List<Resource> incoming = [AgentCard()];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("host-595109fb", incoming[0].Name);
    }

    [Fact]
    public void An_id_match_always_beats_a_mac_match() {
        // The scan card was renamed by the user; a rescan must follow its own id to
        // the rename, not rediscover it via the MAC of some other card.
        SystemResource renamed = ScanCard(name: "storage-primary");
        List<Resource> existing = [renamed, AgentCard(macs: _mac)];
        List<Resource> incoming = [ScanCard()];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("storage-primary", incoming[0].Name);
        Assert.NotNull(incoming[0].DiscoveryId);
    }

    [Fact]
    public void A_mac_claimed_by_two_stored_cards_identifies_nothing() {
        // Ambiguity never unifies: fall through to the ordinary name rules.
        List<Resource> existing = [
            AgentCard(name: "clone-a", machineId: "machine-a"),
            AgentCard(name: "clone-b", machineId: "machine-b")
        ];
        List<Resource> incoming = [ScanCard(name: "host-xyz")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("host-xyz", incoming[0].Name);
        Assert.NotNull(incoming[0].DiscoveryId);
    }

    [Fact]
    public void Two_agent_grade_identities_sharing_a_mac_never_unify() {
        // Cloned VMs can share a NIC MAC while having distinct machine-ids; the
        // machine-id is the authority between agent-grade collectors.
        List<Resource> existing = [AgentCard(name: "vm-a", machineId: "machine-a")];
        List<Resource> incoming = [AgentCard(name: "vm-b", machineId: "machine-b")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("vm-b", incoming[0].Name);
    }

    [Fact]
    public void A_mac_on_a_different_kind_of_card_is_not_a_bridge() {
        // The user put a mac label on the Server card describing the box's hardware;
        // the scan's System card is a different kind of thing and stays separate.
        var server = new Server {
            Kind = "Server",
            Name = "rack-server",
            Labels = { ["mac"] = _mac }
        };

        List<Resource> incoming = [ScanCard(name: "host-xyz")];

        DiscoveryIdResolver.ResolveNames([server], incoming);

        Assert.Equal("host-xyz", incoming[0].Name);
    }

    [Fact]
    public void A_hand_written_card_with_a_mac_label_is_adopted_like_a_name_match() {
        // No id on the stored card: whoever arrives first with an identity stamps it.
        var handWritten = new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = "nas01",
            Type = "baremetal",
            Labels = { ["mac"] = _mac }
        };

        List<Resource> incoming = [ScanCard(name: "host-xyz")];

        DiscoveryIdResolver.ResolveNames([handWritten], incoming);

        Assert.Equal("nas01", incoming[0].Name);
        Assert.NotNull(incoming[0].DiscoveryId); // the scan id gets stamped on
    }
}
