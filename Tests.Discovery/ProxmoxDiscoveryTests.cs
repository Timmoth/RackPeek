using System.Text.Json;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Captured Proxmox API responses in, RackPeek resources out. A node becomes two
///     things — the machine and the hypervisor on it — so most of these are about the
///     tree rather than the fields.
/// </summary>
public class ProxmoxDiscoveryTests {
    private const string _scope = "homelab";

    private static List<Resource> Discover() {
        ProxmoxNode node = ProxmoxResponseParser.ParseNodeStatus(Fixture.Read("pve-node-status.json"), "pve01")
            with {
            Disks = ProxmoxResponseParser.ParseDisks(Fixture.Read("pve-disks.json")),
            Gpus = ProxmoxResponseParser.ParseGpus(Fixture.Read("pve-hardware-pci.json"))
        };

        List<ProxmoxGuest> qemu = ProxmoxResponseParser.ParseGuests(
            Fixture.Read("pve-qemu.json"), "pve01", ProxmoxResponseParser.VmType);

        List<ProxmoxGuest> lxc = ProxmoxResponseParser.ParseGuests(
            Fixture.Read("pve-lxc.json"), "pve01", ProxmoxResponseParser.ContainerType);

        // The command reads each guest's config; here the same two are applied by hand.
        ProxmoxGuestConfig vmConfig = ProxmoxResponseParser.ParseGuestConfig(Fixture.Read("pve-qemu-config.json"));
        ProxmoxGuestConfig ctConfig = ProxmoxResponseParser.ParseGuestConfig(Fixture.Read("pve-lxc-config.json"));

        List<ProxmoxGuest> guests = [
            ..qemu.Select(g => g with { Os = vmConfig.Os, Disks = vmConfig.DiskBytes }),
            ..lxc.Select(g => g with { Os = ctConfig.Os, Ip = ctConfig.Ip, Disks = ctConfig.DiskBytes })
        ];

        return ProxmoxResourceMapper.ToResources(_scope, [node], guests);
    }

    private static SystemResource System(string name) =>
        Discover().OfType<SystemResource>().Single(r => r.Name == name);

    // ---------------------------------------------------------------- the tree

    [Fact]
    public void A_node_becomes_both_the_machine_and_the_hypervisor_on_it() {
        List<Resource> resources = Discover();

        Server server = Assert.Single(resources.OfType<Server>());
        SystemResource hypervisor = resources.OfType<SystemResource>().Single(r => r.Type == "hypervisor");

        Assert.Equal("pve01", server.Name);
        Assert.Equal("pve01-pve", hypervisor.Name);

        // Hardware -> System, which is the relationship RackPeek is built around.
        Assert.Equal(["pve01"], hypervisor.RunsOn);
    }

    [Fact]
    public void Guests_run_on_the_hypervisor_rather_than_straight_on_the_metal() {
        var guests = Discover()
            .OfType<SystemResource>()
            .Where(r => r.Type != "hypervisor")
            .ToList();

        Assert.NotEmpty(guests);
        Assert.All(guests, g => Assert.Equal(["pve01-pve"], g.RunsOn));
    }

    // ------------------------------------------------------------- the machine

    [Fact]
    public void The_machine_carries_its_processor() {
        Server server = Discover().OfType<Server>().Single();

        Cpu cpu = Assert.Single(server.Cpus!);
        Assert.Equal("AMD Ryzen 5 5600G with Radeon Graphics", cpu.Model);
        Assert.Equal(6, cpu.Cores);
        Assert.Equal(12, cpu.Threads);
    }

    [Fact]
    public void A_two_socket_machine_reports_each_socket_separately() {
        // Proxmox reports totals across the machine, so 2 x 8-core reads as cores=16.
        ProxmoxNode node = new() {
            Name = "dual",
            CpuModel = "Intel Xeon Silver 4208",
            Sockets = 2,
            PhysicalCores = 16,
            Cores = 32
        };

        Server server = ProxmoxResourceMapper.ToResources(_scope, [node], []).OfType<Server>().Single();

        Assert.Equal(2, server.Cpus!.Count);
        Assert.All(server.Cpus, c => {
            Assert.Equal(8, c.Cores);
            Assert.Equal(16, c.Threads);
        });
    }

    [Fact]
    public void The_machine_carries_its_physical_disks_already_classified() {
        Server server = Discover().OfType<Server>().Single();

        List<Drive> drives = server.Drives!;

        Assert.Equal(["nvme", "hdd", "ssd"], drives.Select(d => d.Type));
        Assert.Equal(932, drives[0].Size);
    }

    [Fact]
    public void A_disk_proxmox_cannot_classify_is_left_untyped_rather_than_guessed() {
        List<ProxmoxDisk> disks = ProxmoxResponseParser.ParseDisks(Fixture.Read("pve-disks.json"));

        // The zero-size unknown entry is dropped entirely; a real one would keep its size.
        Assert.Equal(3, disks.Count);
        Assert.DoesNotContain(disks, d => d.Type == "unknown");
    }

    [Fact]
    public void The_machine_carries_the_gpus_bolted_into_it() {
        // Including ones passed through to a guest: the card is still in this machine.
        Server server = Discover().OfType<Server>().Single();

        Assert.Equal(
            ["GeForce RTX 3090", "GeForce RTX 3090", "Raphael"],
            server.Gpus!.Select(g => g.Model));
    }

    [Fact]
    public void Only_display_adapters_count_as_gpus() =>
        // The same PCI list carries an IOMMU and an SMBus controller.
        Assert.Equal(3, ProxmoxResponseParser.ParseGpus(Fixture.Read("pve-hardware-pci.json")).Count);

    [Theory]
    [InlineData("GA102 [GeForce RTX 3090]", "GeForce RTX 3090")]
    [InlineData("AD102 [GeForce RTX 4090]", "GeForce RTX 4090")]
    [InlineData("Raphael", "Raphael")]
    [InlineData("", null)]
    public void A_pci_name_is_reduced_to_the_product_people_know(string input, string? expected) =>
        Assert.Equal(expected, ProxmoxResponseParser.MarketingName(input));

    [Fact]
    public void A_machine_with_no_gpu_says_nothing_rather_than_an_empty_list() {
        ProxmoxNode node = new() { Name = "headless" };

        Assert.Null(ProxmoxResourceMapper.ToResources(_scope, [node], []).OfType<Server>().Single().Gpus);
    }

    [Fact]
    public void The_machine_carries_its_memory() =>
        Assert.Equal(63, Discover().OfType<Server>().Single().Ram?.Size);

    // ---------------------------------------------------------- the hypervisor

    [Fact]
    public void The_hypervisor_reports_what_it_is_running() {
        SystemResource hypervisor = System("pve01-pve");

        Assert.Equal("Proxmox VE 8.2.2", hypervisor.Os);
        Assert.Equal(12, hypervisor.Cores);
        Assert.Equal(63, hypervisor.Ram);
    }

    // --------------------------------------------------------------- the guests

    [Fact]
    public void Qemu_guests_are_vms_and_lxc_guests_are_containers() {
        Assert.Equal("vm", System("docker-01").Type);
        Assert.Equal("container", System("pihole").Type);
    }

    [Fact]
    public void A_stopped_guest_is_still_part_of_the_inventory() =>
        // Unlike a stopped container, a stopped VM is a real system with real resources.
        Assert.Contains(Discover(), r => r.Name == "windows-vm");

    [Fact]
    public void A_guest_carries_its_allocation() {
        SystemResource vm = System("docker-01");

        Assert.Equal(4, vm.Cores);
        Assert.Equal(8, vm.Ram);
        Assert.Equal("Linux", vm.Os);
    }

    [Fact]
    public void Every_disk_on_a_guest_is_recorded_not_just_the_boot_one() {
        // maxdisk in the guest list is the boot disk alone, so a VM with a small root
        // and a large data volume would otherwise be recorded at a fraction of its size.
        List<Drive> drives = System("docker-01").Drives!;

        Assert.Equal([64, 2048], drives.Select(d => d.Size));
    }

    [Fact]
    public void Container_mount_points_count_as_disks_too() {
        List<Drive> drives = System("pihole").Drives!;

        Assert.Equal([8, 100], drives.Select(d => d.Size));
    }

    [Theory]
    [InlineData("local-lvm:vm-104-disk-1,iothread=1,size=64G", 68719476736L)]
    [InlineData("tank:vm-104-disk-0,backup=0,size=2T", 2199023255552L)]
    [InlineData("local-lvm:vm-104-disk-0,efitype=4m,size=4M", 4194304L)]
    [InlineData("local-lvm:vm-104-disk-9", 0L)]
    public void A_volume_definition_yields_its_size(string volume, long expected) =>
        Assert.Equal(expected, ProxmoxResponseParser.ParseSize(volume));

    [Fact]
    public void Firmware_scratch_and_install_media_are_not_storage() {
        // efidisk0 is a few megabytes of EFI variables and ide2 is a mounted ISO;
        // neither belongs on an inventory, and the detached unused0 has no size at all.
        List<Drive> drives = System("docker-01").Drives!;

        Assert.DoesNotContain(drives, d => d.Size < 8);
        Assert.Equal(2, drives.Count);
    }

    [Fact]
    public void A_container_with_a_static_address_keeps_it() {
        SystemResource container = System("pihole");

        Assert.Equal("192.168.1.53", container.Ip);
        Assert.Equal("Debian", container.Os);
    }

    [Fact]
    public void A_dhcp_container_reports_no_address_rather_than_a_wrong_one() {
        ProxmoxGuestConfig config = ProxmoxResponseParser.ParseGuestConfig(Fixture.Read("pve-lxc-config-dhcp.json"));

        Assert.Null(config.Ip);
        Assert.Equal("Alpine", config.Os);
    }

    [Fact]
    public void Proxmox_tags_carry_across() {
        Assert.Equal(["production", "web"], System("docker-01").Tags);
        Assert.Equal(["dns"], System("pihole").Tags);
    }

    [Fact]
    public void A_guest_name_with_spaces_becomes_a_usable_one() =>
        Assert.Contains(Discover(), r => r.Name == "paperless-stack");

    [Fact]
    public void An_unnamed_guest_is_identified_by_its_vmid() =>
        Assert.Contains(Discover(), r => r.Name == "vm-110");

    // ------------------------------------------------------------------ identity

    [Fact]
    public void Identity_survives_a_rename_and_a_migration_between_nodes() {
        ProxmoxGuest onFirstNode = new() { VmId = 104, Node = "pve01", Name = "docker-01", Type = "vm" };
        ProxmoxGuest afterMoving = new() { VmId = 104, Node = "pve02", Name = "renamed", Type = "vm" };

        var first = ProxmoxResourceMapper.ToResources(_scope, [], [onFirstNode]).Single().DiscoveryId;
        var second = ProxmoxResourceMapper.ToResources(_scope, [], [afterMoving]).Single().DiscoveryId;

        // The vmid is unique cluster-wide, so neither the node nor the name is part of it.
        Assert.Equal(first, second);
    }

    [Fact]
    public void The_same_vmid_in_a_different_cluster_is_a_different_machine() {
        ProxmoxGuest guest = new() { VmId = 104, Node = "pve01", Name = "docker-01", Type = "vm" };

        Assert.NotEqual(
            ProxmoxResourceMapper.ToResources("homelab", [], [guest]).Single().DiscoveryId,
            ProxmoxResourceMapper.ToResources("office", [], [guest]).Single().DiscoveryId);
    }

    [Fact]
    public void The_machine_and_the_hypervisor_on_it_are_separate_identities() {
        List<Resource> resources = Discover();

        Assert.NotEqual(
            resources.OfType<Server>().Single().DiscoveryId,
            resources.OfType<SystemResource>().Single(r => r.Type == "hypervisor").DiscoveryId);
    }

    [Fact]
    public void A_guest_reported_by_two_nodes_at_once_is_still_one_resource() {
        ProxmoxGuest leaving = new() { VmId = 104, Node = "pve01", Name = "docker-01", Type = "vm" };
        ProxmoxGuest arriving = new() { VmId = 104, Node = "pve02", Name = "docker-01", Type = "vm" };

        Assert.Single(ProxmoxResourceMapper.ToResources(_scope, [], [leaving, arriving]));
    }

    [Fact]
    public void No_two_resources_ever_share_an_identity() {
        List<Resource> resources = Discover();

        Assert.Equal(resources.Select(r => r.DiscoveryId).Distinct().Count(), resources.Count);
    }

    [Fact]
    public void A_guest_named_after_its_node_does_not_take_the_node_name() {
        ProxmoxNode node = new() { Name = "pve01", Cores = 4, MemoryBytes = 8589934592, Version = "pve-manager/8.2.2/x" };
        ProxmoxGuest guest = new() { VmId = 104, Node = "pve01", Name = "pve01", Type = "vm" };

        List<Resource> resources = ProxmoxResourceMapper.ToResources(_scope, [node], [guest]);

        Assert.Equal("pve01", resources[0].Name);
        Assert.Equal(3, resources.Count);
        Assert.Equal(3, resources.Select(r => r.Name).Distinct().Count());
    }

    // -------------------------------------------------------- restricted tokens

    [Fact]
    public void A_restricted_token_still_yields_the_node_list() {
        // What the real API returns for a token without the rights to read node detail.
        List<ProxmoxNode> nodes = ProxmoxResponseParser.ParseNodes(Fixture.Read("pve-nodes.json"));

        ProxmoxNode node = Assert.Single(nodes);
        Assert.Equal("kepler", node.Name);
        Assert.Equal(0, node.Cores);
    }

    [Fact]
    public void A_permitted_token_gets_the_node_sizing_from_the_same_call() {
        List<ProxmoxNode> nodes = ProxmoxResponseParser.ParseNodes(Fixture.Read("pve-nodes-full.json"));

        Assert.Equal(["pve01", "pve02"], nodes.Select(n => n.Name));
        Assert.Equal(12, nodes[0].Cores);
        Assert.Equal(67438305280, nodes[0].MemoryBytes);
    }

    [Fact]
    public void A_node_with_no_readable_detail_still_gives_a_usable_tree() {
        ProxmoxNode bare = new() { Name = "kepler" };
        ProxmoxGuest guest = new() { VmId = 104, Node = "kepler", Name = "docker-01", Type = "vm", Os = "Linux" };

        List<Resource> resources = ProxmoxResourceMapper.ToResources(_scope, [bare], [guest]);

        Server server = resources.OfType<Server>().Single();
        Assert.Equal("kepler", server.Name);
        Assert.Null(server.Cpus);
        Assert.Null(server.Drives);

        // The guest still hangs off the hypervisor, which is what makes the run worth it.
        Assert.Equal(["kepler-pve"], resources.OfType<SystemResource>().Single(r => r.Type == "vm").RunsOn);
    }

    // --------------------------------------------------------------- conformance

    [Fact]
    public void Types_are_ones_the_schema_accepts() =>
        Assert.All(
            Discover().OfType<SystemResource>(),
            r => Assert.Contains(r.Type, SystemResource.ValidSystemTypes));

    [Fact]
    public void A_cluster_names_the_scope_and_a_standalone_host_falls_back_to_its_node() {
        Assert.Equal("homelab",
            ProxmoxResponseParser.ParseIdentityScope(Fixture.Read("pve-cluster-status.json"), "pve01"));

        Assert.Equal("pve01",
            ProxmoxResponseParser.ParseIdentityScope(
                Fixture.Read("pve-cluster-status-standalone.json"), "pve01"));
    }

    [Fact]
    public void Output_conforms_to_the_published_schema() =>
        Fixture.AssertConformsToSchema(DiscoveryDocument.ToYaml(Discover()));

    // ------------------------------------------------- passthrough assignment

    private static List<Resource> DiscoverWithPassthrough() {
        ProxmoxNode node = new() {
            Name = "kepler",
            Cores = 32,
            MemoryBytes = 100_000_000_000,
            Version = "pve-manager/9.2.2/x",
            Gpus = ProxmoxResponseParser.ParseGpus(Fixture.Read("pve-hardware-pci.json"))
        };

        ProxmoxGuestConfig config =
            ProxmoxResponseParser.ParseGuestConfig(Fixture.Read("pve-qemu-config-passthrough.json"));

        ProxmoxGuest guest = new() {
            VmId = 100,
            Node = "kepler",
            Name = "ai",
            Type = "vm",
            Cores = 8,
            MemoryBytes = 34_359_738_368,
            Os = config.Os,
            Disks = config.DiskBytes,
            PassthroughAddresses = config.PassthroughAddresses
        };

        return ProxmoxResourceMapper.ToResources(_scope, [node], [guest]);
    }

    [Fact]
    public void A_guest_records_the_cards_it_has_been_given() {
        SystemResource guest = DiscoverWithPassthrough().OfType<SystemResource>().Single(r => r.Type == "vm");

        Assert.Equal(
            "GeForce RTX 3090, GeForce RTX 3090",
            guest.Labels[ProxmoxResourceMapper.GpuLabel]);
    }

    [Fact]
    public void The_card_itself_still_belongs_to_the_machine_it_is_installed_in() {
        List<Resource> resources = DiscoverWithPassthrough();

        // The guest records the assignment; the Server records the hardware.
        Server server = resources.OfType<Server>().Single();

        Assert.Equal(3, server.Gpus!.Count);
        Assert.DoesNotContain(server.Labels, l => l.Key == ProxmoxResourceMapper.GpuLabel);
    }

    [Fact]
    public void A_config_address_without_a_function_suffix_still_matches_the_device() {
        // The config writes 0000:01:00; the PCI list reports 0000:01:00.0.
        IReadOnlyList<string> addresses = ProxmoxResponseParser.ParseGuestConfig(
            Fixture.Read("pve-qemu-config-passthrough.json")).PassthroughAddresses;

        Assert.Equal(["0000:01:00", "0000:02:00"], addresses);
    }

    [Fact]
    public void Options_after_the_address_are_not_part_of_it() {
        using var document = JsonDocument.Parse(
            """{"hostpci0":"0000:01:00,pcie=1,x-vga=1"}""");

        Assert.Equal(["0000:01:00"], ProxmoxResponseParser.ParsePassthrough(document.RootElement));
    }

    [Fact]
    public void A_guest_holding_nothing_carries_no_label() =>
        Assert.All(Discover().OfType<SystemResource>(), g => Assert.Empty(g.Labels));

    [Fact]
    public void Passthrough_of_something_that_is_not_a_display_adapter_is_ignored() {
        ProxmoxNode node = new() {
            Name = "kepler",
            Gpus = ProxmoxResponseParser.ParseGpus(Fixture.Read("pve-hardware-pci.json"))
        };

        // 0000:00:14.0 is the SMBus controller in the same fixture.
        ProxmoxGuest guest = new() {
            VmId = 100,
            Node = "kepler",
            Name = "hba",
            Type = "vm",
            PassthroughAddresses = ["0000:00:14.0"]
        };

        SystemResource mapped = ProxmoxResourceMapper.ToResources(_scope, [node], [guest])
            .OfType<SystemResource>().Single(r => r.Type == "vm");

        Assert.Empty(mapped.Labels);
    }

    [Fact]
    public void The_same_address_on_a_different_node_is_a_different_card() {
        ProxmoxNode withCards = new() {
            Name = "kepler",
            Gpus = ProxmoxResponseParser.ParseGpus(Fixture.Read("pve-hardware-pci.json"))
        };

        ProxmoxNode headless = new() { Name = "nebula" };

        // Every machine has a 0000:01:00; a guest on the headless node holds nothing.
        ProxmoxGuest guest = new() {
            VmId = 100,
            Node = "nebula",
            Name = "elsewhere",
            Type = "vm",
            PassthroughAddresses = ["0000:01:00"]
        };

        SystemResource mapped = ProxmoxResourceMapper
            .ToResources(_scope, [withCards, headless], [guest])
            .OfType<SystemResource>().Single(r => r.Type == "vm");

        Assert.Empty(mapped.Labels);
    }

    [Fact]
    public void A_guest_holding_more_cards_than_a_label_can_hold_is_truncated() {
        var many = Enumerable.Range(0, 20)
            .Select(i => new ProxmoxGpu($"0000:{i:00}:00.0", "NVIDIA RTX A6000 Ada Generation"))
            .ToList();

        ProxmoxNode node = new() { Name = "dense", Gpus = many };

        ProxmoxGuest guest = new() {
            VmId = 100,
            Node = "dense",
            Name = "trainer",
            Type = "vm",
            PassthroughAddresses = many.Select(g => g.Address).ToList()
        };

        var label = ProxmoxResourceMapper.ToResources(_scope, [node], [guest])
            .OfType<SystemResource>().Single(r => r.Type == "vm")
            .Labels[ProxmoxResourceMapper.GpuLabel];

        // RackPeek caps a label value at 200 characters.
        Assert.True(label.Length <= 200, $"len={label.Length}");
        Assert.False(label.EndsWith(','));
    }

    [Fact]
    public void Output_with_passthrough_conforms_to_the_published_schema() =>
        Fixture.AssertConformsToSchema(DiscoveryDocument.ToYaml(DiscoverWithPassthrough()));
}
