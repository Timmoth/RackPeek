using RackPeek.Domain.Api;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Network-scan output through the real server: pushed over HTTP, merged by the
///     real resolver, asserted against what lands on disk. These pin the identity
///     contracts a scan lives or dies by — idempotent re-runs, renames that stick,
///     and never stealing the identity of a host another collector documented.
/// </summary>
public class NetworkDiscoveryMergeTests {
    private static string ScanYaml(params NetworkHostFact[] hosts) =>
        DiscoveryDocument.ToYaml(NetworkScanMapper.ToResources(hosts));

    private static NetworkHostFact Nas(string ip = "192.168.1.20") =>
        new(ip, "dc:a6:32:0f:11:22", "nas01.lan", true, []);

    [Fact]
    public async Task A_scan_lands_on_disk_and_a_rescan_changes_nothing() {
        using var api = new DiscoveryApiFixture();

        ImportYamlResponse first = await api.PublishAsync(ScanYaml(Nas()));

        Assert.Equal(["nas01"], first.Added);
        Assert.Contains("discoveryId: rpk1:net:", api.StoredYaml);
        Assert.Contains("mac: dc:a6:32:0f:11:22", api.StoredYaml);
        Fixture.AssertConformsToSchema(api.StoredYaml);

        ImportYamlResponse second = await api.PublishAsync(ScanYaml(Nas()));

        Assert.Empty(second.Added);
        Assert.Empty(second.Updated);
    }

    [Fact]
    public async Task A_users_rename_survives_the_next_scan() {
        // The stored card is a previous scan of the same machine that the user has
        // since renamed — the id stayed with it, as the UI keeps it on a rename.
        List<Resource> renamed = NetworkScanMapper.ToResources([Nas()]);
        renamed[0].Name = "storage-primary";

        using var api = new DiscoveryApiFixture(DiscoveryDocument.ToYaml(renamed));

        ImportYamlResponse rescan = await api.PublishAsync(ScanYaml(Nas()));

        Assert.Empty(rescan.Added);
        Assert.Contains("storage-primary", api.StoredYaml);
        Assert.DoesNotContain("name: nas01", api.StoredYaml);
    }

    [Fact]
    public async Task A_dhcp_move_updates_the_address_of_the_same_machine() {
        using var api = new DiscoveryApiFixture();

        await api.PublishAsync(ScanYaml(Nas(ip: "192.168.1.20")));
        ImportYamlResponse moved = await api.PublishAsync(ScanYaml(Nas(ip: "192.168.1.99")));

        Assert.Empty(moved.Added); // same MAC, same machine
        Assert.Equal(["nas01"], moved.Updated);
        Assert.Contains("ip: 192.168.1.99", api.StoredYaml);
        Assert.DoesNotContain("192.168.1.20", api.StoredYaml);
    }

    [Fact]
    public async Task A_scan_never_steals_the_identity_of_an_agent_discovered_host() {
        // nas01 exists with a machine-id identity but WITHOUT the macs label an agent
        // records (an older agent, or a hand-stripped label) — so there is no MAC
        // bridge, and the resolver must keep the cards apart rather than guess.
        var agentDiscovered = DiscoveryDocument.ToYaml([
            new SystemResource {
                Kind = SystemResource.KindLabel,
                Name = "nas01",
                DiscoveryId = DiscoveryId.Create(DiscoveryId.SystemScheme, "machine-a"),
                Type = "baremetal",
                Os = "Debian",
                Cores = 12
            }
        ]);

        using var api = new DiscoveryApiFixture(agentDiscovered);

        ImportYamlResponse response = await api.PublishAsync(ScanYaml(Nas()));

        var scanName = Assert.Single(response.Added);
        Assert.StartsWith("nas01-", scanName); // suffixed, not adopted

        var stored = api.StoredYaml;
        Assert.Contains("rpk1:sys:", stored); // the agent identity is intact
        Assert.Contains("rpk1:net:", stored); // and the scan's card exists beside it
        Assert.Contains("os: Debian", stored); // nothing on the original was touched
    }

    [Fact]
    public async Task An_agent_claims_a_scanned_card_and_every_collector_lands_on_it_after() {
        // The unification headline, scan-first: the sweep found the box, then
        // `rpk discover system` runs on it. Same MAC, so it is the same card — the
        // agent's identity and detail land on the scan's card instead of duplicating.
        using var api = new DiscoveryApiFixture();

        await api.PublishAsync(ScanYaml(Nas()));

        SystemResource agent = SystemResourceMapper.ToResource(new SystemFacts {
            Hostname = "nas01.lan",
            MachineId = "machine-a",
            Os = "Debian 12",
            Cores = 12,
            RamGb = 64,
            Type = "baremetal",
            Ip = "192.168.1.20",
            Macs = ["dc:a6:32:0f:11:22"]
        });

        ImportYamlResponse claim = await api.PublishAsync(DiscoveryDocument.ToYaml([agent]));

        // Nothing added: the agent updated the scan's card (which keeps its name).
        Assert.Empty(claim.Added);
        Assert.Equal(["nas01"], claim.Updated);

        var stored = api.StoredYaml;
        Assert.Contains("rpk1:sys:", stored); // identity upgraded to the agent's
        Assert.DoesNotContain("rpk1:net:", stored);
        Assert.Contains("os: Debian 12", stored);
        Assert.Contains("mac: dc:a6:32:0f:11:22", stored);
        Assert.Contains("macs: dc:a6:32:0f:11:22", stored);
        Fixture.AssertConformsToSchema(stored);

        // ...and a rescan afterwards still lands on that same card via the MAC.
        ImportYamlResponse rescan = await api.PublishAsync(ScanYaml(Nas(ip: "192.168.1.99")));

        Assert.Empty(rescan.Added);
        Assert.Contains("rpk1:sys:", api.StoredYaml); // never downgraded
        Assert.Contains("ip: 192.168.1.99", api.StoredYaml); // but freshly addressed
    }

    [Fact]
    public async Task A_scan_enriches_an_agent_discovered_card_instead_of_duplicating_it() {
        // The reverse order: the agent documented the box first, then a sweep sees it.
        SystemResource agent = SystemResourceMapper.ToResource(new SystemFacts {
            Hostname = "nas01",
            MachineId = "machine-a",
            Os = "Debian 12",
            Cores = 12,
            RamGb = 64,
            Type = "baremetal",
            Macs = ["dc:a6:32:0f:11:22"]
        });

        using var api = new DiscoveryApiFixture(DiscoveryDocument.ToYaml([agent]));

        ImportYamlResponse scan = await api.PublishAsync(ScanYaml(Nas()));

        Assert.Empty(scan.Added);
        Assert.Equal(["nas01"], scan.Updated);

        var stored = api.StoredYaml;
        Assert.Contains("rpk1:sys:", stored); // the agent identity is untouched
        Assert.DoesNotContain("rpk1:net:", stored);
        Assert.Contains("ip: 192.168.1.20", stored); // the scan contributed the address
        Assert.Contains("os: Debian 12", stored);
        Fixture.AssertConformsToSchema(stored);
    }

    [Fact]
    public async Task A_scan_adopts_a_hand_written_system_of_the_same_name() {
        // The inverse case: the user typed the card themselves, so it has no id yet.
        // The scan stamps its identity onto it and enriches it instead of duplicating.
        using var api = new DiscoveryApiFixture(
            """
            version: 4
            resources:
              - kind: System
                name: nas01
                type: baremetal
                os: Debian
            """);

        ImportYamlResponse response = await api.PublishAsync(ScanYaml(Nas()));

        Assert.Empty(response.Added);
        Assert.Equal(["nas01"], response.Updated);

        var stored = api.StoredYaml;
        Assert.Contains("rpk1:net:", stored);
        Assert.Contains("ip: 192.168.1.20", stored);
        // The scan card is sparse on purpose, so everything the user wrote survives.
        Assert.Contains("os: Debian", stored);
        Assert.Contains("type: baremetal", stored);
    }
}
