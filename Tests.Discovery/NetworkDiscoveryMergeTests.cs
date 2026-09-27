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
        // nas01 already exists with a machine-id identity from `rpk discover system`.
        // The scan sees the same box from outside and proposes the same name with a
        // MAC identity — the resolver must keep them apart, not merge one over the other.
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
