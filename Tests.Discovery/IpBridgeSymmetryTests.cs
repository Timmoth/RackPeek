using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     The address bridge has to read the same whichever collector ran first.
///     <para>
///         ARP is link-local, so a sweep of a routed subnet gets an address and no MAC,
///         and the card it writes can only be called <c>host-&lt;hash&gt;</c>. A hypervisor
///         knows that guest by name, by specification, and by the address it holds — so
///         the address is the one thing tying the two together. Whether the sweep or the
///         hypervisor got there first is an accident of what the person typed, and must
///         not decide whether the inventory ends up with one card or two.
///     </para>
///     <para>
///         A live run of nine subnets found this the hard way: sweeping before running
///         the Proxmox and firewall collectors produced fourteen machines twice over,
///         where the same commands in the other order produced one card each.
///     </para>
/// </summary>
public class IpBridgeSymmetryTests {
    private const string _ip = "10.0.50.105";
    private const string _mac = "bc:24:11:00:4a:01";

    /// <summary>What a sweep of a routed subnet can write: an address, and nothing else.</summary>
    private static SystemResource ScanCard(string? ip = _ip) => new() {
        Kind = SystemResource.KindLabel,
        Name = "host-2ed3bfd7",
        DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, $"ip:{ip}"),
        Ip = ip
    };

    /// <summary>What the hypervisor knows about the same box.</summary>
    private static SystemResource GuestCard(string name = "forgejo", string? ip = _ip) => new() {
        Kind = SystemResource.KindLabel,
        Name = name,
        DiscoveryId = DiscoveryId.Create(ProxmoxResourceMapper.Scheme, "vmid-105"),
        Type = "vm",
        Os = "Linux",
        Ip = ip,
        Labels = { ["macs"] = _mac }
    };

    private static void Resolve(List<Resource> existing, List<Resource> incoming) =>
        DiscoveryIdResolver.ResolveNames(existing, incoming, null, null, true);

    [Fact]
    public void The_hypervisor_claims_the_card_a_sweep_left_behind() {
        // Sweep ran first. This is the direction that was silently broken.
        List<Resource> existing = [ScanCard()];
        List<Resource> incoming = [GuestCard()];

        Resolve(existing, incoming);

        // One card, not two: the guest lands on the stored one.
        Assert.Equal(existing[0].Name, incoming[0].Name);
    }

    [Fact]
    public void The_sweep_lands_on_the_card_the_hypervisor_left_behind() {
        // The direction that already worked, kept honest.
        List<Resource> existing = [GuestCard()];
        List<Resource> incoming = [ScanCard()];

        Resolve(existing, incoming);

        Assert.Equal("forgejo", incoming[0].Name);
    }

    [Fact]
    public void The_agent_identity_wins_whichever_way_round_it_arrives() {
        // Arriving second, the hypervisor's id survives so the merge can upgrade the
        // stored card from a stand-in to a real identity.
        List<Resource> incoming = [GuestCard()];
        Resolve([ScanCard()], incoming);
        Assert.StartsWith("rpk1:pve:", incoming[0].DiscoveryId);

        // Arriving second, the sweep's id is dropped so the merge cannot downgrade it.
        incoming = [ScanCard()];
        Resolve([GuestCard()], incoming);
        Assert.Null(incoming[0].DiscoveryId);
    }

    [Fact]
    public void The_placeholder_name_gives_way_to_the_real_one() {
        // The point of unifying: the machine stops being a hash. This is the upgrade
        // that could never fire while the cards stayed separate.
        List<Resource> existing = [ScanCard()];

        Resolve(existing, [GuestCard()]);

        Assert.Equal("forgejo", existing[0].Name);
    }

    [Fact]
    public void Two_stand_ins_at_one_address_still_unify_nothing() {
        // Neither side knows anything the other does not, so an address proves nothing.
        List<Resource> existing = [ScanCard()];
        SystemResource other = ScanCard();
        other.Name = "host-9999aaaa";
        other.DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, "ip:other");

        Resolve(existing, [other]);

        Assert.Equal("host-9999aaaa", other.Name);
    }

    [Fact]
    public void Two_agent_grade_cards_at_one_address_still_unify_nothing() {
        // A guest and the machine-id of the OS inside it are both agent-grade. They are
        // two cards on purpose; an address must not collapse them.
        var fromAgent = new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = "forgejo-inside",
            DiscoveryId = DiscoveryId.Create(DiscoveryId.SystemScheme, "machine-a"),
            Ip = _ip
        };

        Resolve([GuestCard()], [fromAgent]);

        Assert.Equal("forgejo-inside", fromAgent.Name);
    }

    [Fact]
    public void A_sweep_that_saw_a_mac_is_left_to_the_mac_bridge() {
        // A MAC is better evidence than an address, whichever side is holding it. A scan
        // card with one either unified on the MAC already or genuinely disagrees.
        SystemResource scan = ScanCard();
        scan.Labels["mac"] = "bc:24:11:00:4a:99";

        Resolve([scan], [GuestCard()]);

        Assert.Equal("host-2ed3bfd7", scan.Name);
    }

    [Fact]
    public void A_different_kind_of_thing_at_the_same_address_is_not_the_same_thing() {
        // The box documented as hardware and the OS a sweep saw on it are separate cards
        // on purpose — the merge replaces on a type change, so unifying would delete one.
        var server = new Server {
            Kind = Server.KindLabel,
            Name = "kepler",
            DiscoveryId = DiscoveryId.Create(ProxmoxResourceMapper.Scheme, "node-kepler")
        };

        List<Resource> existing = [ScanCard()];

        Resolve(existing, [server]);

        Assert.Equal("kepler", server.Name);
        Assert.Equal("host-2ed3bfd7", existing[0].Name);
    }

    [Fact]
    public void An_address_two_stored_cards_claim_unifies_nothing() {
        // Overlapping subnets, or a stale card. Ambiguity is not evidence.
        SystemResource first = ScanCard();
        SystemResource second = ScanCard();
        second.Name = "host-bbbbcccc";
        second.DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, "ip:dup");

        List<Resource> incoming = [GuestCard()];

        Resolve([first, second], incoming);

        Assert.Equal("forgejo", incoming[0].Name);
    }
}
