using RackPeek.Domain.Api;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     The promise discovery has to keep: run it as often as you like, from a timer,
///     and it updates what is already there instead of piling up duplicates — even
///     after the resource has been renamed by hand.
///     Every test here goes over HTTP into a real server and asserts on the stored YAML.
/// </summary>
public class DiscoveryMergeTests {
    private static SystemFacts Facts(
        string machineId = "machine-a",
        string hostname = "nas01",
        double ramGb = 63) {
        return new SystemFacts {
            Hostname = hostname,
            MachineId = machineId,
            Os = "Debian GNU/Linux 12 (bookworm)",
            Cores = 12,
            RamGb = ramGb,
            Type = "baremetal",
            Ip = "192.168.1.20"
        };
    }

    private static string SystemYaml(
        string machineId = "machine-a",
        string hostname = "nas01",
        double ramGb = 63) =>
        DiscoveryDocument.ToYaml([SystemResourceMapper.ToResource(Facts(machineId, hostname, ramGb))]);

    private static string IdFor(string machineId) =>
        DiscoveryId.Create(DiscoveryId.SystemScheme, machineId);

    [Fact]
    public async Task First_run_adds_the_machine() {
        using var api = new DiscoveryApiFixture();

        ImportYamlResponse response = await api.PublishAsync(SystemYaml());

        Assert.Equal(["nas01"], response.Added);
        Assert.Contains($"discoveryId: {IdFor("machine-a")}", api.StoredYaml);
    }

    [Fact]
    public async Task A_v3_config_still_imports_and_is_saved_as_v4() {
        using var api = new DiscoveryApiFixture();

        // discoveryId arrived with schema v4; a pre-discovery v3 file must keep working.
        ImportYamlResponse response = await api.PublishAsync("""
                                                             version: 3
                                                             resources:
                                                               - kind: System
                                                                 name: nas01
                                                                 type: baremetal
                                                                 os: Debian
                                                                 cores: 12
                                                                 ram: 63
                                                             """);

        Assert.Equal(["nas01"], response.Added);
        Assert.StartsWith("version: 4", api.StoredYaml);
    }

    [Fact]
    public async Task Running_again_updates_rather_than_duplicating() {
        using var api = new DiscoveryApiFixture();

        await api.PublishAsync(SystemYaml());
        ImportYamlResponse second = await api.PublishAsync(SystemYaml(ramGb: 127));

        Assert.Empty(second.Added);
        Assert.Equal(["nas01"], second.Updated);
        Assert.Equal(1, Count(api.StoredYaml, "name: nas01"));
        Assert.Contains("ram: 127", api.StoredYaml);
    }

    [Fact]
    public async Task A_machine_renamed_by_hand_is_still_found_by_its_id() {
        using var api = new DiscoveryApiFixture();

        // What the inventory looks like after the user renamed it in the web UI.
        await api.PublishAsync($"""
                                version: 3
                                resources:
                                  - kind: System
                                    name: storage-01
                                    type: baremetal
                                    os: Debian GNU/Linux 12 (bookworm)
                                    cores: 12
                                    ram: 63
                                    discoveryId: {IdFor("machine-a")}
                                """);

        // The machine itself still reports its hostname.
        ImportYamlResponse response = await api.PublishAsync(SystemYaml(ramGb: 127));

        Assert.Empty(response.Added);
        Assert.Equal(["storage-01"], response.Updated);
        Assert.DoesNotContain("nas01", api.StoredYaml);
        Assert.Contains("ram: 127", api.StoredYaml);
    }

    [Fact]
    public async Task A_hand_written_resource_of_the_same_name_is_adopted_not_duplicated() {
        using var api = new DiscoveryApiFixture();

        await api.PublishAsync("""
                               version: 3
                               resources:
                                 - kind: System
                                   name: nas01
                                   type: baremetal
                                   os: Debian
                                   cores: 12
                                   ram: 63
                                   notes: bought in 2019
                               """);

        ImportYamlResponse response = await api.PublishAsync(SystemYaml());

        Assert.Empty(response.Added);
        Assert.Equal(["nas01"], response.Updated);
        Assert.Equal(1, Count(api.StoredYaml, "name: nas01"));

        // Adoption keeps what the user wrote, and the resource gains an identity.
        Assert.Contains("2019", api.StoredYaml);
        Assert.Contains($"discoveryId: {IdFor("machine-a")}", api.StoredYaml);
    }

    [Fact]
    public async Task A_second_machine_with_the_same_hostname_does_not_hijack_the_first() {
        using var api = new DiscoveryApiFixture();

        await api.PublishAsync(SystemYaml("machine-a"));
        ImportYamlResponse response = await api.PublishAsync(SystemYaml("machine-b"));

        // The newcomer stands aside rather than overwriting, and both are kept.
        Assert.Single(response.Added);
        Assert.StartsWith("nas01-", response.Added[0]);
        Assert.Equal(1, Count(api.StoredYaml, "name: nas01\n"));
        Assert.Contains($"name: {response.Added[0]}", api.StoredYaml);
    }

    [Fact]
    public async Task Services_follow_the_host_when_it_has_been_renamed() {
        using var api = new DiscoveryApiFixture();

        await api.PublishAsync($"""
                                version: 3
                                resources:
                                  - kind: System
                                    name: storage-01
                                    type: baremetal
                                    os: Debian
                                    cores: 12
                                    ram: 63
                                    discoveryId: {IdFor("machine-a")}
                                """);

        // Docker discovery on that machine still knows it only by its hostname. The
        // payload below — host System first, then its services — is exactly what
        // DiscoverDockerCommand sends for a local engine; the host rides along because
        // this rename could not be reconciled from the services' bare runsOn names.
        SystemResource host = SystemResourceMapper.ToResource(Facts());

        List<Service> services = DockerServiceMapper.ToResources(
            DockerContainerParser.Parse(Fixture.Read("docker-containers.json")),
            "machine-a",
            host.Name,
            "192.168.1.20");

        await api.PublishAsync(DiscoveryDocument.ToYaml([host, .. services]));

        // runsOn was rewritten to the name the user chose, so the tree is not broken.
        Assert.Contains("- storage-01", api.StoredYaml);
        Assert.DoesNotContain("nas01", api.StoredYaml);
    }

    [Fact]
    public async Task A_remote_engines_services_follow_a_rename_they_cannot_see() {
        // A remote collector sends services only — no host System rides along, because
        // the facts it probes locally describe the wrong machine. What the inventory
        // looks like after the user documented the host and renamed it:
        using var api = new DiscoveryApiFixture($"""
                                                 version: 4
                                                 resources:
                                                   - kind: System
                                                     name: storage-01
                                                     type: baremetal
                                                     os: Debian
                                                     cores: 12
                                                     ram: 63
                                                     discoveryId: {IdFor("machine-a")}
                                                   - kind: Service
                                                     name: jellyfin
                                                     discoveryId: {DiscoveryId.Create(DiscoveryId.DockerScheme, "engine-a/jellyfin")}
                                                     network:
                                                       ip: 192.168.1.20
                                                       port: 8096
                                                       protocol: TCP
                                                     runsOn:
                                                       - storage-01
                                                 """);

        // The engine still reports its hostname, which no longer names anything here.
        Service jellyfin = DockerServiceMapper.ToResources(
                DockerContainerParser.Parse(Fixture.Read("docker-containers.json")),
                "engine-a",
                "nas01",
                "192.168.1.20")
            .Single(s => s.Name == "jellyfin");

        await api.PublishAsync(DiscoveryDocument.ToYaml([jellyfin]));

        // The user's link survived the re-discovery; the stale hostname did not land.
        Assert.Contains("- storage-01", api.StoredYaml);
        Assert.Equal(1, Count(api.StoredYaml, "name: jellyfin"));
        Assert.DoesNotContain("- nas01", api.StoredYaml);
    }

    [Fact]
    public async Task A_dry_run_reports_the_change_without_making_it() {
        using var api = new DiscoveryApiFixture();

        ImportYamlResponse response = await api.PublishAsync(SystemYaml(), true);

        Assert.Equal(["nas01"], response.Added);
        Assert.DoesNotContain("nas01", api.StoredYaml);
    }

    [Fact]
    public async Task Machines_sharing_a_cloned_machine_id_are_rejected_rather_than_silently_merged() {
        using var api = new DiscoveryApiFixture();

        var yaml = DiscoveryDocument.ToYaml([
            SystemResourceMapper.ToResource(Facts("clone", "vm-a")),
            SystemResourceMapper.ToResource(Facts("clone", "vm-b"))
        ]);

        InvalidOperationException error =
            await Assert.ThrowsAsync<InvalidOperationException>(() => api.PublishAsync(yaml));

        Assert.Contains("machine-id", error.Message);
    }

    private static int Count(string haystack, string needle) {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);

        while (index >= 0) {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    [Fact]
    public async Task Discovering_a_machine_does_not_destroy_hardware_documented_under_the_same_name() {
        using var api = new DiscoveryApiFixture();

        // A very ordinary starting point: the box was documented as hardware by hand.
        await api.PublishAsync("""
                               version: 3
                               resources:
                                 - kind: Server
                                   name: nas01
                                   notes: 4U chassis, bought 2019
                                   ram:
                                     size: 64
                               """);

        ImportYamlResponse response = await api.PublishAsync(SystemYaml());

        // The hardware is untouched...
        Assert.Contains("kind: Server", api.StoredYaml);
        Assert.Contains("4U chassis", api.StoredYaml);

        // ...and the operating system is recorded alongside it rather than instead of it.
        Assert.Single(response.Added);
        Assert.StartsWith("nas01-", response.Added[0]);
        Assert.Contains("kind: System", api.StoredYaml);
    }

    [Fact]
    public async Task Many_machines_pushing_at_once_do_not_lose_each_other() {
        // The realistic shape of this feature: a fleet on the same nightly timer, all
        // arriving within the same second. A read-modify-write that is not serialised
        // would silently drop most of them.
        using var api = new DiscoveryApiFixture();

        const int machines = 12;

        await Task.WhenAll(Enumerable.Range(0, machines)
            .Select(i => api.PublishAsync(SystemYaml($"machine-{i}", $"box-{i:00}"))));

        var stored = api.StoredYaml;

        for (var i = 0; i < machines; i++) {
            Assert.Contains($"name: box-{i:00}", stored);
            Assert.Contains($"discoveryId: {IdFor($"machine-{i}")}", stored);
        }

        Assert.Equal(machines, Count(stored, "kind: System"));
    }

    [Fact]
    public async Task Repeated_runs_converge_rather_than_accumulating() {
        using var api = new DiscoveryApiFixture();

        for (var i = 0; i < 10; i++)
            await api.PublishAsync(SystemYaml(ramGb: 60 + i));

        Assert.Equal(1, Count(api.StoredYaml, "kind: System"));
        Assert.Contains("ram: 69", api.StoredYaml);
    }

    private static string ProxmoxYaml(string guestName = "docker-01") {
        ProxmoxNode node = new() {
            Name = "pve01",
            Cores = 12,
            MemoryBytes = 67438305280,
            Version = "pve-manager/8.2.2/x"
        };

        ProxmoxGuest guest = new() {
            VmId = 104,
            Node = "pve01",
            Name = guestName,
            Type = "vm",
            Cores = 4,
            MemoryBytes = 8589934592,
            Os = "Linux"
        };

        return DiscoveryDocument.ToYaml(ProxmoxResourceMapper.ToResources("homelab", [node], [guest]));
    }

    [Fact]
    public async Task A_proxmox_estate_arrives_with_its_tree_intact() {
        using var api = new DiscoveryApiFixture();

        ImportYamlResponse response = await api.PublishAsync(ProxmoxYaml());

        // The machine, the hypervisor on it, and the guest on that.
        Assert.Equal(["pve01", "pve01-pve", "docker-01"], response.Added);

        // The relationships are the tedious part to type, so they have to survive.
        Assert.Contains("kind: Server", api.StoredYaml);
        Assert.Contains("- pve01\n", api.StoredYaml);
        Assert.Contains("- pve01-pve", api.StoredYaml);
    }

    [Fact]
    public async Task A_guest_renamed_in_proxmox_updates_rather_than_duplicating() {
        using var api = new DiscoveryApiFixture();

        await api.PublishAsync(ProxmoxYaml());
        ImportYamlResponse response = await api.PublishAsync(ProxmoxYaml("docker-renamed"));

        // The vmid is the identity, so renaming the guest in Proxmox does not make a
        // second resource — and the name the user sees in RackPeek is left alone.
        Assert.Empty(response.Added);
        Assert.Equal(1, Count(api.StoredYaml, "type: vm"));
        Assert.DoesNotContain("docker-renamed", api.StoredYaml);
    }

    [Fact]
    public async Task Known_limitation_a_guest_discovered_twice_over_is_two_resources() {
        // Proxmox identifies a guest by vmid; the guest identifies itself by machine-id.
        // Neither can derive the other, so running both collectors over the same machine
        // produces two resources. Documented rather than silently surprising: the second
        // one is reported as an addition with a suffixed name, not merged into the first.
        using var api = new DiscoveryApiFixture();

        await api.PublishAsync(ProxmoxYaml());
        ImportYamlResponse response = await api.PublishAsync(SystemYaml(hostname: "docker-01"));

        Assert.Single(response.Added);
        Assert.StartsWith("docker-01-", response.Added[0]);
    }
}
