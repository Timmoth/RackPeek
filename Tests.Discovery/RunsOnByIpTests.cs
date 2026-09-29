using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Giving a service the host it is plainly running on, when the collector could not.
///     A docker collector talking to a remote engine has to guess its host's name and the
///     link then dangles; the service's own address is evidence the guess is not. The
///     rules that keep this from doing harm are what these tests own.
/// </summary>
public class RunsOnByIpTests {
    private static SystemResource System(string name, string? ip, string? discoveryId = null) =>
        new() {
            Kind = SystemResource.KindLabel,
            Name = name,
            Ip = ip,
            DiscoveryId = discoveryId
        };

    private static Service Service(string name, string? ip, string? runsOn = null, string? discoveryId = "rpk1:docker:1") =>
        new() {
            Kind = RackPeek.Domain.Resources.Services.Service.KindLabel,
            Name = name,
            DiscoveryId = discoveryId,
            Network = ip == null ? null : new Network { Ip = ip },
            RunsOn = runsOn == null ? [] : [runsOn]
        };

    [Fact]
    public void A_service_whose_host_name_matches_nothing_is_anchored_to_the_system_at_its_address() {
        // The remote-docker case: the engine called itself NAS01.lan, the inventory
        // calls that machine nas01, and nothing linked the two.
        List<Resource> existing = [System("nas01", "192.0.2.50")];
        List<Resource> incoming = [Service("immich", "192.0.2.50", "NAS01.lan")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["nas01"], incoming.OfType<Service>().Single().RunsOn);
    }

    [Fact]
    public void A_service_with_no_host_at_all_is_anchored_too() {
        List<Resource> existing = [System("nas01", "192.0.2.50")];
        List<Resource> incoming = [Service("immich", "192.0.2.50")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["nas01"], incoming.OfType<Service>().Single().RunsOn);
    }

    [Fact]
    public void A_host_arriving_in_the_same_payload_counts() {
        // discover docker emits the host alongside its services.
        List<Resource> incoming = [
            System("nas01", "192.0.2.50", "rpk1:sys:a"),
            Service("immich", "192.0.2.50", "somewhere-else")
        ];

        DiscoveryIdResolver.ResolveNames([], incoming);

        Assert.Equal(["nas01"], incoming.OfType<Service>().Single().RunsOn);
    }

    [Fact]
    public void A_working_host_link_is_never_overwritten() {
        // The service says it runs on a resource that exists. That is the collector's
        // own answer and it beats an inference drawn from an address.
        List<Resource> existing = [
            System("nas01", "192.0.2.50"),
            System("some-vm", "192.0.2.99")
        ];
        List<Resource> incoming = [Service("immich", "192.0.2.50", "some-vm")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["some-vm"], incoming.OfType<Service>().Single().RunsOn);
    }

    [Fact]
    public void A_stored_host_beats_a_scanned_stand_in_at_the_same_address() {
        // Once a hypervisor reports its guests' addresses, a sweep of that subnet finds
        // the same machines again and contributes a sparse card per address. The service
        // belongs on the guest the hypervisor described, not on the sweep's stand-in.
        List<Resource> existing = [System("app-vm", "192.0.2.50")];
        List<Resource> incoming = [
            System("host-1a2b3c4d", "192.0.2.50", "rpk1:net:c"),
            Service("immich", "192.0.2.50", "SOMEWHERE.lan")
        ];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["app-vm"], incoming.OfType<Service>().Single().RunsOn);
    }

    [Fact]
    public void An_address_two_systems_claim_anchors_nothing() {
        // Overlapping subnets across sites, or a stale card nobody cleaned up. An
        // ambiguous address is no evidence, and a wrong parent is worse than none.
        List<Resource> existing = [
            System("site-a-host", "192.0.2.50"),
            System("site-b-host", "192.0.2.50")
        ];
        List<Resource> incoming = [Service("immich", "192.0.2.50", "NAS01.lan")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["NAS01.lan"], incoming.OfType<Service>().Single().RunsOn);
    }

    [Fact]
    public void A_service_with_no_address_is_left_alone() {
        List<Resource> existing = [System("nas01", "192.0.2.50")];
        List<Resource> incoming = [Service("immich", null, "NAS01.lan")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["NAS01.lan"], incoming.OfType<Service>().Single().RunsOn);
    }

    [Fact]
    public void An_address_no_system_claims_anchors_nothing() {
        List<Resource> existing = [System("nas01", "192.0.2.50")];
        List<Resource> incoming = [Service("immich", "198.51.100.99", "NAS01.lan")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["NAS01.lan"], incoming.OfType<Service>().Single().RunsOn);
    }

    [Fact]
    public void Systems_are_not_anchored_by_address() {
        // Two systems sharing an address are the same machine or a conflict — never a
        // parent and a child. Only services are hosted.
        List<Resource> existing = [System("nas01", "192.0.2.50")];
        List<Resource> incoming = [System("scanned-host", "192.0.2.50", "rpk1:net:b")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Empty(incoming.OfType<SystemResource>().Single().RunsOn);
    }

    // ---------------------------------------------------------------
    // Unifying a sweep's find with the guest a hypervisor already described
    // ---------------------------------------------------------------

    private static SystemResource Scanned(string name, string ip, string? mac = null) {
        var card = new SystemResource {
            Kind = SystemResource.KindLabel,
            Name = name,
            Ip = ip,
            DiscoveryId = DiscoveryId.Create(DiscoveryId.NetworkScheme, mac ?? $"ip:{ip}")
        };

        if (mac != null)
            card.Labels["mac"] = mac;

        return card;
    }

    private static SystemResource Guest(string name, string ip, string id = "abc123") =>
        new() {
            Kind = SystemResource.KindLabel,
            Name = name,
            Ip = ip,
            DiscoveryId = $"rpk1:pve:{id}"
        };

    [Fact]
    public void A_sweep_find_becomes_the_guest_the_hypervisor_already_described() {
        // ARP is link-local, so a guest on another subnet gives the sweep no MAC and its
        // identity falls back to the address. Now that the hypervisor reports that same
        // address, it is the only thing tying the two records together.
        List<Resource> existing = [Guest("app-vm", "192.0.2.105")];
        List<Resource> incoming = [Scanned("host-1a2b3c4d", "192.0.2.105")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        SystemResource card = incoming.OfType<SystemResource>().Single();
        Assert.Equal("app-vm", card.Name);
        // The sweep's weaker identity is dropped so the merge cannot downgrade the
        // hypervisor's.
        Assert.Null(card.DiscoveryId);
    }

    [Fact]
    public void A_sweep_find_that_saw_a_mac_is_left_to_the_mac_rule() {
        // A MAC is better evidence than an address. If it did not unify above, the two
        // records disagree, and an address must not override that.
        List<Resource> existing = [Guest("app-vm", "192.0.2.105")];
        List<Resource> incoming = [Scanned("host-1a2b3c4d", "192.0.2.105", "bc:24:11:00:1a:01")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("host-1a2b3c4d", incoming.OfType<SystemResource>().Single().Name);
    }

    [Fact]
    public void Two_stored_systems_at_one_address_unify_nothing() {
        // Overlapping subnets across sites, or a stale card. Ambiguity is not evidence.
        List<Resource> existing = [
            Guest("site-a-vm", "192.0.2.105", "aaa111"),
            Guest("site-b-vm", "192.0.2.105", "bbb222")
        ];
        List<Resource> incoming = [Scanned("host-1a2b3c4d", "192.0.2.105")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("host-1a2b3c4d", incoming.OfType<SystemResource>().Single().Name);
    }

    [Fact]
    public void Two_sweep_finds_with_nothing_between_them_are_the_same_card_already() {
        // Neither card is more than "something replied at this address", and an address
        // is exactly what seeds their identity when no MAC was seen — so they carry the
        // same id and the address rule never gets a say. The stored name wins, as it
        // does for any re-run.
        List<Resource> existing = [Scanned("host-99999999", "192.0.2.105")];
        List<Resource> incoming = [Scanned("host-1a2b3c4d", "192.0.2.105")];

        Assert.Equal(existing[0].DiscoveryId, incoming[0].DiscoveryId);

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("host-99999999", incoming.OfType<SystemResource>().Single().Name);
    }

    [Fact]
    public void A_sweep_find_folds_into_one_that_saw_the_mac() {
        // Not two stand-ins: the stored card names the NIC answering at that address —
        // a firewall's neighbour table does this for every subnet it routes — where the
        // incoming one only knows something replied. The MAC is the better identity, so
        // the address-only card folds into it rather than becoming a second machine.
        List<Resource> existing = [Scanned("host-99999999", "192.0.2.105", "bc:24:11:00:1a:09")];
        List<Resource> incoming = [Scanned("host-1a2b3c4d", "192.0.2.105")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        SystemResource card = incoming.OfType<SystemResource>().Single();
        Assert.Equal("host-99999999", card.Name);

        // The weaker address-seeded identity is dropped so the merge cannot downgrade
        // the MAC-seeded one it is landing on.
        Assert.Null(card.DiscoveryId);
    }

    [Fact]
    public void The_mac_wins_arriving_second_too() {
        // Sweep the routed subnet first, ask the firewall after: same two facts, same
        // one card. Here the MAC-seeded identity is the one that survives.
        List<Resource> existing = [Scanned("host-1a2b3c4d", "192.0.2.105")];
        List<Resource> incoming = [Scanned("host-99999999", "192.0.2.105", "bc:24:11:00:1a:09")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        SystemResource card = incoming.OfType<SystemResource>().Single();
        Assert.Equal("host-1a2b3c4d", card.Name);
        Assert.NotNull(card.DiscoveryId);
    }

    [Fact]
    public void Two_sweep_finds_that_each_saw_a_mac_never_unify() {
        // Both name a NIC, and they name different ones. The MAC bridge has already had
        // its say; sharing an address now is a conflict or an overlapping subnet.
        List<Resource> existing = [Scanned("host-99999999", "192.0.2.105", "bc:24:11:00:1a:09")];
        List<Resource> incoming = [Scanned("host-1a2b3c4d", "192.0.2.105", "bc:24:11:00:1a:0a")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal("host-1a2b3c4d", incoming.OfType<SystemResource>().Single().Name);
    }
}
