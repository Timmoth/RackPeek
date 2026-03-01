using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.UpsUnits;
using RackPeek.Domain.Templates;

namespace Tests.Unit.Templates;

public class BundledHardwareTemplateStoreTests : IDisposable
{
    private readonly string _tempDir;

    public BundledHardwareTemplateStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "rackpeek-template-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void WriteTemplate(string kindPlural, string fileName, string yaml)
    {
        var dir = Path.Combine(_tempDir, kindPlural);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, fileName), yaml);
    }

    [Fact]
    public async Task get_all_async__with_templates__returns_all()
    {
        // Arrange
        WriteTemplate("switches", "test-switch.yaml", """
            kind: Switch
            name: test-switch
            model: TestSwitch-24
            managed: true
            poe: true
            ports:
              - type: rj45
                speed: 1
                count: 24
            """);
        WriteTemplate("routers", "test-router.yaml", """
            kind: Router
            name: test-router
            model: TestRouter-4
            managed: true
            poe: false
            ports:
              - type: rj45
                speed: 1
                count: 4
            """);
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var templates = await sut.GetAllAsync();

        // Assert
        Assert.Equal(2, templates.Count);
    }

    [Fact]
    public async Task get_all_by_kind_async__filters_by_kind()
    {
        // Arrange
        WriteTemplate("switches", "sw1.yaml", """
            kind: Switch
            name: sw1
            model: Switch-A
            ports:
              - type: rj45
                speed: 1
                count: 8
            """);
        WriteTemplate("routers", "rt1.yaml", """
            kind: Router
            name: rt1
            model: Router-A
            ports:
              - type: rj45
                speed: 1
                count: 4
            """);
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var switches = await sut.GetAllByKindAsync("Switch");
        var routers = await sut.GetAllByKindAsync("Router");

        // Assert
        Assert.Single(switches);
        Assert.Equal("Switch-A", switches[0].Model);
        Assert.Single(routers);
        Assert.Equal("Router-A", routers[0].Model);
    }

    [Fact]
    public async Task get_by_id_async__existing_template__returns_template()
    {
        // Arrange
        WriteTemplate("firewalls", "Netgate-6100.yaml", """
            kind: Firewall
            name: Netgate-6100
            model: Netgate-6100
            managed: true
            poe: false
            ports:
              - type: rj45
                speed: 1
                count: 4
              - type: sfp+
                speed: 10
                count: 2
            """);
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var template = await sut.GetByIdAsync("Firewall/Netgate-6100");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Firewall", template.Kind);
        Assert.Equal("Netgate-6100", template.Model);
        Assert.IsType<Firewall>(template.Spec);
        var fw = (Firewall)template.Spec;
        Assert.True(fw.Managed);
        Assert.Equal(2, fw.Ports!.Count);
    }

    [Fact]
    public async Task get_by_id_async__nonexistent_template__returns_null()
    {
        // Arrange
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var template = await sut.GetByIdAsync("Switch/DoesNotExist");

        // Assert
        Assert.Null(template);
    }

    [Fact]
    public async Task get_all_async__nonexistent_directory__returns_empty()
    {
        // Arrange
        var sut = new BundledHardwareTemplateStore(Path.Combine(_tempDir, "no-such-dir"));

        // Act
        var templates = await sut.GetAllAsync();

        // Assert
        Assert.Empty(templates);
    }

    [Fact]
    public async Task get_all_by_kind_async__case_insensitive()
    {
        // Arrange
        WriteTemplate("switches", "sw.yaml", """
            kind: Switch
            name: sw
            model: TestSwitch
            ports:
              - type: rj45
                speed: 1
                count: 4
            """);
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var result = await sut.GetAllByKindAsync("switch");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task switch_template__preserves_port_details()
    {
        // Arrange
        WriteTemplate("switches", "usw.yaml", """
            kind: Switch
            name: usw
            model: USW-Enterprise
            managed: true
            poe: true
            ports:
              - type: rj45
                speed: 1
                count: 12
              - type: sfp+
                speed: 10
                count: 4
            """);
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var template = await sut.GetByIdAsync("Switch/USW-Enterprise");

        // Assert
        Assert.NotNull(template);
        var sw = Assert.IsType<Switch>(template.Spec);
        Assert.Equal(2, sw.Ports!.Count);
        Assert.Equal("rj45", sw.Ports[0].Type);
        Assert.Equal(1, sw.Ports[0].Speed);
        Assert.Equal(12, sw.Ports[0].Count);
        Assert.Equal("sfp+", sw.Ports[1].Type);
        Assert.Equal(10, sw.Ports[1].Speed);
        Assert.Equal(4, sw.Ports[1].Count);
    }

    [Fact]
    public async Task accesspoint_template__preserves_speed()
    {
        // Arrange
        WriteTemplate("accesspoints", "u6.yaml", """
            kind: AccessPoint
            name: u6
            model: UniFi-U6-Pro
            speed: 2.5
            """);
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var template = await sut.GetByIdAsync("AccessPoint/UniFi-U6-Pro");

        // Assert
        Assert.NotNull(template);
        var ap = Assert.IsType<AccessPoint>(template.Spec);
        Assert.Equal(2.5, ap.Speed);
    }

    [Fact]
    public async Task ups_template__preserves_va()
    {
        // Arrange
        WriteTemplate("ups", "apc.yaml", """
            kind: Ups
            name: apc
            model: APC-2200
            va: 2200
            """);
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var template = await sut.GetByIdAsync("Ups/APC-2200");

        // Assert
        Assert.NotNull(template);
        var ups = Assert.IsType<Ups>(template.Spec);
        Assert.Equal(2200, ups.Va);
    }

    [Fact]
    public async Task server_template__preserves_cpu_ram_drives_nics_ipmi()
    {
        // Arrange
        WriteTemplate("servers", "Intel-NUC-13-Pro.yaml", """
            kind: Server
            name: Intel-NUC-13-Pro
            cpus:
              - model: Intel Core i7-1360P
                cores: 12
                threads: 16
            ram:
              size: 32
              mts: 4800
            drives:
              - type: nvme
                size: 1024
            nics:
              - type: rj45
                speed: 2.5
                ports: 1
            ipmi: false
            """);
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var template = await sut.GetByIdAsync("Server/Intel-NUC-13-Pro");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Server", template.Kind);
        Assert.Equal("Intel-NUC-13-Pro", template.Model);
        var server = Assert.IsType<Server>(template.Spec);
        Assert.False(server.Ipmi);
        Assert.NotNull(server.Ram);
        Assert.Equal(32, server.Ram.Size);
        Assert.Equal(4800, server.Ram.Mts);
        Assert.Single(server.Cpus!);
        Assert.Equal("Intel Core i7-1360P", server.Cpus![0].Model);
        Assert.Equal(12, server.Cpus[0].Cores);
        Assert.Equal(16, server.Cpus[0].Threads);
        Assert.Single(server.Drives!);
        Assert.Equal("nvme", server.Drives![0].Type);
        Assert.Equal(1024, server.Drives[0].Size);
        Assert.Single(server.Nics!);
        Assert.Equal("rj45", server.Nics![0].Type);
        Assert.Equal(2.5, server.Nics[0].Speed);
        Assert.Equal(1, server.Nics[0].Ports);
    }

    [Fact]
    public async Task malformed_yaml__skipped_gracefully()
    {
        // Arrange
        WriteTemplate("switches", "good.yaml", """
            kind: Switch
            name: good
            model: Good-Switch
            ports:
              - type: rj45
                speed: 1
                count: 8
            """);
        WriteTemplate("switches", "bad.yaml", "this is: [not valid yaml: {{}");
        var sut = new BundledHardwareTemplateStore(_tempDir);

        // Act
        var templates = await sut.GetAllAsync();

        // Assert
        Assert.Single(templates);
        Assert.Equal("Good-Switch", templates[0].Model);
    }
}
