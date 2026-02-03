using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Yaml;

namespace Tests.Yaml;

public class HardwareDeserializationTests
{
    public static IHardwareRepository CreateSut(string yaml)
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "RackPeekTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, "config.yaml");
        File.WriteAllText(filePath, yaml);

        var yamlResourceCollection = new YamlResourceCollection(filePath);
        return new YamlHardwareRepository(yamlResourceCollection);
    }


    [Theory]
    [InlineData("Server", typeof(Server))]
    [InlineData("Switch", typeof(Switch))]
    [InlineData("Firewall", typeof(Firewall))]
    [InlineData("Desktop", typeof(Desktop))]
    [InlineData("Laptop", typeof(Laptop))]
    [InlineData("Router", typeof(Router))]
    [InlineData("AccessPoint", typeof(AccessPoint))]
    [InlineData("Ups", typeof(Ups))]
    public async Task deserialize_yaml_kind(string kind, Type type)
    {
        // Given
        var yaml = $@"
resources:
  - kind: {kind}
";

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();

        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType(type, hardware);
    }

    [Fact]
    public async Task deserialize_yaml_kind_server()
    {
        // Given
        var yaml = @"
resources:
  - kind: Server
    name: dell-c6400-node01
    cpus:
        - model: Intel(R) Xeon(R) CPU E3-1270 v6
          cores: 4
          threads: 8
    ram:
        size: 32gb
        Mts: 2400
    drives:
        - type: hdd
          size: 2Tb
        - type: ssd
          size: 256gb
    gpus:
        - model: NVIDIA Tesla T4
          vram: 16gb
    nics:
        - type: rj45
          speed: 1gb
          ports: 2
        - type: sfp+
          speed: 10gb
          ports: 2
    ipmi: true
";
        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();


        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType<Server>(hardware);
        var server = hardware as Server;
        Assert.NotNull(server);
        Assert.Equal("dell-c6400-node01", server.Name);
        // Cpu
        Assert.NotNull(server.Cpus);
        var cpu = server.Cpus[0];
        Assert.Equal("Intel(R) Xeon(R) CPU E3-1270 v6", cpu.Model);
        Assert.Equal(4, cpu.Cores);
        Assert.Equal(8, cpu.Threads);

        // Ram
        Assert.NotNull(server.Ram);
        Assert.Equal(32, server.Ram.Size);
        Assert.Equal(2400, server.Ram.Mts);

        // Drives
        Assert.NotNull(server.Drives);
        var hdd = server.Drives[0];
        Assert.Equal("hdd", hdd.Type);
        Assert.Equal(2048, hdd.Size);
        var ssd = server.Drives[1];
        Assert.Equal("ssd", ssd.Type);
        Assert.Equal(256, ssd.Size);

        //GPUs
        Assert.NotNull(server.Gpus);
        var gpu = server.Gpus[0];
        Assert.Equal("NVIDIA Tesla T4", gpu.Model);
        Assert.Equal(16, gpu.Vram);

        // ipmi
        Assert.True(server.Ipmi);

        // Nics
        Assert.NotNull(server.Nics);
        var nic0 = server.Nics[0];
        Assert.Equal("rj45", nic0.Type);
        Assert.Equal(1, nic0.Speed);
        Assert.Equal(2, nic0.Ports);
        var nic1 = server.Nics[1];
        Assert.Equal("sfp+", nic1.Type);
        Assert.Equal(10, nic1.Speed);
        Assert.Equal(2, nic1.Ports);
    }

    [Fact]
    public async Task deserialize_yaml_kind_switch()
    {
        // Given
        var yaml = @"
resources:
  - kind: Switch
    name: netgear-s24
    model: GS324
    ports:
        - type: rj45
          speed: 1gb
          count: 8
        - type: sfp
          speed: 10gb
          count: 2
    managed: true
    poe: true
";

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();


        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType<Switch>(hardware);

        var sw = hardware as Switch;
        Assert.NotNull(sw);

        Assert.Equal("netgear-s24", sw.Name);
        Assert.Equal("GS324", sw.Model);
        Assert.Equal(true, sw.Managed);
        Assert.Equal(true, sw.Poe);

        // Nics
        Assert.NotNull(sw.Ports);
        var nic0 = sw.Ports[0];
        Assert.Equal("rj45", nic0.Type);
        Assert.Equal(1, nic0.Speed);
        Assert.Equal(8, nic0.Count);
        var nic1 = sw.Ports[1];
        Assert.Equal("sfp", nic1.Type);
        Assert.Equal(10, nic1.Speed);
        Assert.Equal(2, nic1.Count);
    }

    [Fact]
    public async Task deserialize_yaml_kind_firewall()
    {
        // Given
        var yaml = @"
resources:
  - kind: Firewall
    name: pfsense
    model: pfSense-1100
    ports:
        - type: rj45
          speed: 1gb
          count: 8
        - type: sfp
          speed: 10gb
          count: 2
    managed: true
    poe: true
";

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();


        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType<Firewall>(hardware);

        var fw = hardware as Firewall;
        Assert.NotNull(fw);

        Assert.Equal("pfsense", fw.Name);
        Assert.Equal("pfSense-1100", fw.Model);
        Assert.Equal(true, fw.Managed);
        Assert.Equal(true, fw.Poe);

        // Nics
        Assert.NotNull(fw.Ports);
        var nic0 = fw.Ports[0];
        Assert.Equal("rj45", nic0.Type);
        Assert.Equal(1, nic0.Speed);
        Assert.Equal(8, nic0.Count);
        var nic1 = fw.Ports[1];
        Assert.Equal("sfp", nic1.Type);
        Assert.Equal(10, nic1.Speed);
        Assert.Equal(2, nic1.Count);
    }

    [Fact]
    public async Task deserialize_yaml_kind_router()
    {
        // Given
        var yaml = @"
resources:
  - kind: Router
    name: ubiquiti-edge-router
    model: ER-4
    ports:
        - type: rj45
          speed: 1gb
          count: 8
        - type: sfp
          speed: 10gb
          count: 2
    managed: true
    poe: true
";

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();


        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType<Router>(hardware);

        var router = hardware as Router;
        Assert.NotNull(router);

        Assert.Equal("ubiquiti-edge-router", router.Name);
        Assert.Equal("ER-4", router.Model);
        Assert.Equal(true, router.Managed);
        Assert.Equal(true, router.Poe);

        // Nics
        Assert.NotNull(router.Ports);
        var nic0 = router.Ports[0];
        Assert.Equal("rj45", nic0.Type);
        Assert.Equal(1, nic0.Speed);
        Assert.Equal(8, nic0.Count);
        var nic1 = router.Ports[1];
        Assert.Equal("sfp", nic1.Type);
        Assert.Equal(10, nic1.Speed);
        Assert.Equal(2, nic1.Count);
    }

    [Fact]
    public async Task deserialize_yaml_kind_desktop()
    {
        // Given
        var yaml = @"
resources:
  - kind: Desktop
    name: dell-optiplex
    cpus:
      - model: Intel(R) Core(TM) i5-9500
        cores: 6
        threads: 6
    ram:
      size: 16gb
      mts: 2666
    drives:
      - type: ssd
        size: 512gb
    nics:
      - type: rj45
        speed: 1gb
        ports: 1
    gpus:
       - model: RTX 3080
         vram: 12gb
";

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();


        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType<Desktop>(hardware);

        var desktop = hardware as Desktop;
        Assert.NotNull(desktop);

        Assert.Equal("dell-optiplex", desktop.Name);

        // CPU
        Assert.NotNull(desktop.Cpus);
        Assert.Equal("Intel(R) Core(TM) i5-9500", desktop.Cpus[0].Model);
        Assert.Equal(6, desktop.Cpus[0].Cores);
        Assert.Equal(6, desktop.Cpus[0].Threads);

        // RAM
        Assert.NotNull(desktop.Ram);
        Assert.Equal(16, desktop.Ram.Size);
        Assert.Equal(2666, desktop.Ram.Mts);

        // Drives
        Assert.NotNull(desktop.Drives);
        Assert.Equal("ssd", desktop.Drives[0].Type);
        Assert.Equal(512, desktop.Drives[0].Size);

        // NIC
        Assert.NotNull(desktop.Nics);
        Assert.Equal("rj45", desktop.Nics[0].Type);
        Assert.Equal(1, desktop.Nics[0].Speed);
        Assert.Equal(1, desktop.Nics[0].Ports);
    }

    [Fact]
    public async Task deserialize_yaml_kind_laptop()
    {
        // Given
        var yaml = @"
resources:
  - kind: Laptop
    name: thinkpad-x1
    cpus:
        - model: Intel(R) Core(TM) i7-10510U
          cores: 4
          threads: 8
    ram:
        size: 16gb
        mts: 2666
    drives:
        - type: ssd
          size: 1tb
    gpus:
       - model: RTX 3080
         vram: 12gb
";

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();


        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType<Laptop>(hardware);

        var laptop = hardware as Laptop;
        Assert.NotNull(laptop);

        Assert.Equal("thinkpad-x1", laptop.Name);

        // CPU
        Assert.NotNull(laptop.Cpus);
        Assert.Equal("Intel(R) Core(TM) i7-10510U", laptop.Cpus[0].Model);
        Assert.Equal(4, laptop.Cpus[0].Cores);
        Assert.Equal(8, laptop.Cpus[0].Threads);

        // RAM
        Assert.NotNull(laptop.Ram);
        Assert.Equal(16, laptop.Ram.Size);
        Assert.Equal(2666, laptop.Ram.Mts);

        // Drives
        Assert.NotNull(laptop.Drives);
        Assert.Equal("ssd", laptop.Drives[0].Type);
        Assert.Equal(1024, laptop.Drives[0].Size);
    }

    [Fact]
    public async Task deserialize_yaml_kind_accesspoint()
    {
        // Given
        var yaml = @"
resources:
  - kind: AccessPoint
    name: lounge-ap
    model: Unifi-Ap-Pro
    speed: 2.5Gb
";

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();


        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType<AccessPoint>(hardware);

        var accessPoint = hardware as AccessPoint;
        Assert.NotNull(accessPoint);

        Assert.Equal("lounge-ap", accessPoint.Name);
        Assert.Equal("Unifi-Ap-Pro", accessPoint.Model);
        Assert.Equal(2.5, accessPoint.Speed);
    }

    [Fact]
    public async Task deserialize_yaml_kind_ups()
    {
        // Given
        var yaml = @"
resources:
  - kind: Ups
    name: rack-ups
    model: Volta
    va: 2200
";

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();


        // Then
        var hardware = Assert.Single(resources);
        Assert.IsType<Ups>(hardware);

        var ups = hardware as Ups;
        Assert.NotNull(ups);

        Assert.Equal("rack-ups", ups.Name);
        Assert.Equal("Volta", ups.Model);
        Assert.Equal(2200, ups.Va);
    }
}