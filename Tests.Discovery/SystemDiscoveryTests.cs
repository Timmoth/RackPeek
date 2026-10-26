using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Host snapshot in, RackPeek YAML out. Nothing here touches the machine the tests
///     run on, so a Linux host is inspected identically from macOS or Windows.
/// </summary>
public class SystemDiscoveryTests {
    private static RawSystemSnapshot LinuxSnapshot(
        string? machineId = "7f3c9a1e5b2d4f6081a3c5e7b9d1f3a5",
        string? cgroup = null,
        string? dmiVendor = "Dell Inc.",
        string? dmiProduct = "PowerEdge R730",
        string hostname = "nas01.lan") {
        return new RawSystemSnapshot {
            Hostname = hostname,
            Cores = 12,
            FallbackMemoryBytes = 1024L * 1024 * 1024,
            OsReleaseFile = Fixture.Read("linux-os-release"),
            MemInfoFile = Fixture.Read("linux-meminfo"),
            MachineIdFile = machineId,
            CgroupFile = cgroup ?? Fixture.Read("linux-cgroup-host"),
            DmiVendor = dmiVendor,
            DmiProduct = dmiProduct,
            Nics = [
                new NicFact("lo", true, true, false, "127.0.0.1"),
                new NicFact("docker0", true, false, false, "172.17.0.1"),
                new NicFact("eno1", true, false, true, "192.168.1.20")
            ],
            BlockDevices = [
                new BlockDeviceFact("nvme0n1", 1_000_204_886_016, false),
                new BlockDeviceFact("sda", 8_001_563_222_016, true),
                new BlockDeviceFact("loop0", 67_108_864, false),
                new BlockDeviceFact("sr0", 0, true)
            ]
        };
    }

    [Fact]
    public void Linux_host_maps_onto_a_complete_system_resource() {
        SystemFacts facts = SystemFactsParser.Parse(LinuxSnapshot());
        SystemResource resource = SystemResourceMapper.ToResource(facts);

        Assert.Equal("nas01", resource.Name);
        Assert.Equal("Debian GNU/Linux 12 (bookworm)", resource.Os);
        Assert.Equal("baremetal", resource.Type);
        Assert.Equal(12, resource.Cores);
        Assert.Equal("192.168.1.20", resource.Ip);
        Assert.StartsWith("rpk1:sys:", resource.DiscoveryId);
    }

    [Fact]
    public void Ram_comes_from_meminfo_which_reports_a_little_under_the_physical_total() {
        // 65790000 kB is what a 64 GB machine reports once the kernel has taken its share.
        // Reported as measured rather than rounded up to the DIMM size it was sold as.
        SystemFacts facts = SystemFactsParser.Parse(LinuxSnapshot());

        Assert.Equal(63, facts.RamGb);
    }

    [Fact]
    public void Physical_disks_are_kept_and_kernel_devices_are_not() {
        SystemFacts facts = SystemFactsParser.Parse(LinuxSnapshot());

        Assert.Collection(facts.Drives,
            nvme => {
                Assert.Equal("nvme", nvme.Type);
                Assert.Equal(932, nvme.SizeGb);
            },
            hdd => {
                Assert.Equal("hdd", hdd.Type);
                Assert.Equal(7452, hdd.SizeGb);
            });
    }

    [Fact]
    public void Routable_interface_wins_over_loopback_and_docker_bridge() =>
        Assert.Equal("192.168.1.20", SystemFactsParser.Parse(LinuxSnapshot()).Ip);

    [Fact]
    public void Falls_back_to_a_real_interface_when_none_advertises_a_gateway() {
        RawSystemSnapshot snapshot = LinuxSnapshot() with {
            Nics = [
                new NicFact("lo", true, true, false, "127.0.0.1"),
                new NicFact("docker0", true, false, false, "172.17.0.1"),
                new NicFact("eno1", true, false, false, "192.168.1.20")
            ]
        };

        Assert.Equal("192.168.1.20", SystemFactsParser.Parse(snapshot).Ip);
    }

    [Theory]
    [InlineData("QEMU", "Standard PC (i440FX + PIIX, 1996)", "vm")]
    [InlineData("VMware, Inc.", "VMware Virtual Platform", "vm")]
    [InlineData("Microsoft Corporation", "Virtual Machine", "vm")]
    [InlineData("innotek GmbH", "VirtualBox", "vm")]
    [InlineData("Dell Inc.", "PowerEdge R730", "baremetal")]
    [InlineData("Supermicro", "X11SSH-F", "baremetal")]
    public void Virtualisation_is_read_from_dmi(string vendor, string product, string expected) {
        RawSystemSnapshot snapshot = LinuxSnapshot(dmiVendor: vendor, dmiProduct: product);

        Assert.Equal(expected, SystemFactsParser.Parse(snapshot).Type);
    }

    [Fact]
    public void A_container_is_detected_from_its_cgroup() {
        RawSystemSnapshot snapshot = LinuxSnapshot(cgroup: Fixture.Read("linux-cgroup-container"));

        Assert.Equal("container", SystemFactsParser.Parse(snapshot).Type);
    }

    [Fact]
    public void A_container_does_not_claim_the_host_disks_it_can_see() {
        // /sys/block inside a container shows the machine underneath it.
        RawSystemSnapshot snapshot = LinuxSnapshot(cgroup: Fixture.Read("linux-cgroup-container"));

        SystemFacts facts = SystemFactsParser.Parse(snapshot);

        Assert.Equal("container", facts.Type);
        Assert.Empty(facts.Drives);
    }

    [Fact]
    public void A_container_is_detected_from_the_dockerenv_marker() {
        RawSystemSnapshot snapshot = LinuxSnapshot() with { DockerEnvPresent = true };

        Assert.Equal("container", SystemFactsParser.Parse(snapshot).Type);
    }

    [Fact]
    public void Type_is_one_of_the_values_the_schema_accepts() {
        SystemFacts facts = SystemFactsParser.Parse(LinuxSnapshot());

        Assert.Contains(facts.Type, SystemResource.ValidSystemTypes);
    }

    [Fact]
    public void Macos_reads_its_identity_out_of_ioreg() {
        var uuid = MacSystemProbe.ParsePlatformUuid(Fixture.Read("macos-ioreg.txt"));

        Assert.Equal("5C8E1F2A-3B4D-5E6F-7A8B-9C0D1E2F3A4B", uuid);
    }

    [Fact]
    public void Macos_host_maps_without_disks_rather_than_guessing_at_them() {
        var snapshot = new RawSystemSnapshot {
            Hostname = "tims-macbook-pro.local",
            Cores = 14,
            OsName = "macOS 15.7.3",
            MemoryBytes = 51_539_607_552,
            PlatformUuid = "5C8E1F2A-3B4D-5E6F-7A8B-9C0D1E2F3A4B",
            Nics = [new NicFact("en0", true, false, true, "10.0.20.157")]
        };

        SystemResource resource = SystemResourceMapper.ToResource(SystemFactsParser.Parse(snapshot));

        Assert.Equal("tims-macbook-pro", resource.Name);
        Assert.Equal("macOS 15.7.3", resource.Os);
        Assert.Equal(48, resource.Ram);
        Assert.Equal("baremetal", resource.Type);
        Assert.Null(resource.Drives);
    }

    [Fact]
    public void A_hypervisor_flag_on_macos_means_the_host_is_a_guest() {
        var snapshot = new RawSystemSnapshot {
            Hostname = "vm",
            Cores = 4,
            OsName = "macOS 15.7.3",
            MemoryBytes = 8_589_934_592,
            HypervisorPresent = true
        };

        Assert.Equal("vm", SystemFactsParser.Parse(snapshot).Type);
    }

    [Fact]
    public void An_explicit_name_beats_the_hostname() {
        SystemFacts facts = SystemFactsParser.Parse(LinuxSnapshot());

        Assert.Equal("storage-01", SystemResourceMapper.ToResource(facts, "storage-01").Name);
    }

    [Fact]
    public void Output_conforms_to_the_published_schema() {
        SystemFacts facts = SystemFactsParser.Parse(LinuxSnapshot());

        Fixture.AssertConformsToSchema(
            DiscoveryDocument.ToYaml([SystemResourceMapper.ToResource(facts)]));
    }
}
