using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.UpsUnits;

namespace Tests.Unit.Helpers;

public class ResourceTemplateSerializerTests
{
    [Fact]
    public void serialize__server__includes_kind_name_cpus_ram_drives_nics()
    {
        var server = new Server
        {
            Name = "my-server",
            Kind = "Server",
            Ipmi = true,
            Ram = new Ram { Size = 64, Mts = 5200 },
            Cpus = [new Cpu { Model = "Intel i9-13900H", Cores = 14, Threads = 20 }],
            Drives = [new Drive { Type = "nvme", Size = 1024 }],
            Nics = [new Nic { Type = "rj45", Speed = 2.5, Ports = 2 }],
            Gpus = [new Gpu { Model = "Intel Iris Xe", Vram = 0 }]
        };

        var yaml = ResourceTemplateSerializer.Serialize(server);

        Assert.StartsWith("kind: Server", yaml);
        Assert.Contains("name: my-server", yaml);
        Assert.Contains("ipmi: true", yaml);
        Assert.Contains("model: Intel i9-13900H", yaml);
        Assert.Contains("cores: 14", yaml);
        Assert.Contains("threads: 20", yaml);
        Assert.Contains("size: 64", yaml);
        Assert.Contains("mts: 5200", yaml);
        Assert.Contains("type: nvme", yaml);
        Assert.Contains("speed: 2.5", yaml);
        Assert.Contains("ports: 2", yaml);
        Assert.Contains("model: Intel Iris Xe", yaml);
        Assert.Contains("vram: 0", yaml);
    }

    [Fact]
    public void serialize__switch__includes_kind_model_ports_managed_poe()
    {
        var sw = new Switch
        {
            Name = "core-switch",
            Kind = "Switch",
            Model = "USW-Enterprise-24",
            Managed = true,
            Poe = true,
            Ports =
            [
                new Port { Type = "rj45", Speed = 1, Count = 24 },
                new Port { Type = "sfp+", Speed = 10, Count = 4 }
            ]
        };

        var yaml = ResourceTemplateSerializer.Serialize(sw);

        Assert.StartsWith("kind: Switch", yaml);
        Assert.Contains("name: core-switch", yaml);
        Assert.Contains("model: USW-Enterprise-24", yaml);
        Assert.Contains("managed: true", yaml);
        Assert.Contains("poe: true", yaml);
        Assert.Contains("type: rj45", yaml);
        Assert.Contains("count: 24", yaml);
        Assert.Contains("type: sfp+", yaml);
        Assert.Contains("count: 4", yaml);
    }

    [Fact]
    public void serialize__strips_tags_labels_notes_runsOn()
    {
        var server = new Server
        {
            Name = "tagged-server",
            Kind = "Server",
            Tags = ["production", "rack-1"],
            Labels = new Dictionary<string, string> { ["env"] = "prod", ["dc"] = "us-east" },
            Notes = "These are some notes about the server.",
            RunsOn = ["host-1"],
            Cpus = [new Cpu { Model = "AMD EPYC", Cores = 64, Threads = 128 }]
        };

        var yaml = ResourceTemplateSerializer.Serialize(server);

        Assert.DoesNotContain("tags:", yaml);
        Assert.DoesNotContain("production", yaml);
        Assert.DoesNotContain("labels:", yaml);
        Assert.DoesNotContain("env", yaml);
        Assert.DoesNotContain("notes:", yaml);
        Assert.DoesNotContain("These are some notes", yaml);
        Assert.DoesNotContain("runsOn:", yaml);
        Assert.DoesNotContain("host-1", yaml);
        Assert.Contains("name: tagged-server", yaml);
        Assert.Contains("model: AMD EPYC", yaml);
    }

    [Fact]
    public void serialize__kind_appears_first()
    {
        var router = new Router
        {
            Name = "edge-router",
            Kind = "Router",
            Model = "ER-8",
            Managed = true,
            Ports = [new Port { Type = "rj45", Speed = 1, Count = 8 }]
        };

        var yaml = ResourceTemplateSerializer.Serialize(router);
        var lines = yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.StartsWith("kind:", lines[0]);
        Assert.StartsWith("name:", lines[1]);
    }

    [Fact]
    public void serialize__empty_collections_omitted()
    {
        var server = new Server
        {
            Name = "bare-server",
            Kind = "Server",
            Cpus = null,
            Drives = null,
            Nics = null,
            Gpus = null,
            Ram = null,
            Ipmi = null
        };

        var yaml = ResourceTemplateSerializer.Serialize(server);

        Assert.Contains("kind: Server", yaml);
        Assert.Contains("name: bare-server", yaml);
        Assert.DoesNotContain("cpus:", yaml);
        Assert.DoesNotContain("drives:", yaml);
        Assert.DoesNotContain("nics:", yaml);
        Assert.DoesNotContain("gpus:", yaml);
        Assert.DoesNotContain("ram:", yaml);
        Assert.DoesNotContain("ipmi:", yaml);
    }

    [Fact]
    public void serialize__firewall__includes_type_specific_fields()
    {
        var fw = new Firewall
        {
            Name = "perimeter-fw",
            Kind = "Firewall",
            Model = "Netgate-6100",
            Managed = true,
            Poe = false,
            Ports = [new Port { Type = "sfp+", Speed = 10, Count = 2 }]
        };

        var yaml = ResourceTemplateSerializer.Serialize(fw);

        Assert.StartsWith("kind: Firewall", yaml);
        Assert.Contains("model: Netgate-6100", yaml);
        Assert.Contains("managed: true", yaml);
        Assert.Contains("poe: false", yaml);
        Assert.Contains("type: sfp+", yaml);
    }

    [Fact]
    public void serialize__accesspoint__includes_speed()
    {
        var ap = new AccessPoint
        {
            Name = "lobby-ap",
            Kind = "AccessPoint",
            Model = "U6-Pro",
            Speed = 2.5
        };

        var yaml = ResourceTemplateSerializer.Serialize(ap);

        Assert.StartsWith("kind: Accesspoint", yaml);
        Assert.Contains("model: U6-Pro", yaml);
        Assert.Contains("speed: 2.5", yaml);
    }

    [Fact]
    public void serialize__ups__includes_va()
    {
        var ups = new Ups
        {
            Name = "rack-ups",
            Kind = "Ups",
            Model = "APC-2200",
            Va = 2200
        };

        var yaml = ResourceTemplateSerializer.Serialize(ups);

        Assert.StartsWith("kind: Ups", yaml);
        Assert.Contains("model: APC-2200", yaml);
        Assert.Contains("va: 2200", yaml);
    }

    [Fact]
    public void serialize__desktop__includes_hardware_details()
    {
        var desktop = new Desktop
        {
            Name = "dev-desktop",
            Kind = "Desktop",
            Model = "OptiPlex-7090",
            Cpus = [new Cpu { Model = "Intel i7-11700", Cores = 8, Threads = 16 }],
            Ram = new Ram { Size = 32, Mts = 3200 }
        };

        var yaml = ResourceTemplateSerializer.Serialize(desktop);

        Assert.StartsWith("kind: Desktop", yaml);
        Assert.Contains("model: OptiPlex-7090", yaml);
        Assert.Contains("model: Intel i7-11700", yaml);
        Assert.Contains("size: 32", yaml);
    }

    [Fact]
    public void serialize__laptop__includes_hardware_details()
    {
        var laptop = new Laptop
        {
            Name = "work-laptop",
            Kind = "Laptop",
            Model = "ThinkPad-X1",
            Cpus = [new Cpu { Model = "Intel i7-1365U", Cores = 10, Threads = 12 }],
            Drives = [new Drive { Type = "nvme", Size = 512 }]
        };

        var yaml = ResourceTemplateSerializer.Serialize(laptop);

        Assert.StartsWith("kind: Laptop", yaml);
        Assert.Contains("model: ThinkPad-X1", yaml);
        Assert.Contains("type: nvme", yaml);
        Assert.Contains("size: 512", yaml);
    }

    [Fact]
    public void serialize__does_not_mutate_original_resource()
    {
        var server = new Server
        {
            Name = "original",
            Kind = "Server",
            Tags = ["keep-this"],
            Labels = new Dictionary<string, string> { ["keep"] = "this" },
            Notes = "keep these notes",
            RunsOn = ["host-1"],
            Cpus = [new Cpu { Model = "Test", Cores = 4, Threads = 8 }]
        };

        ResourceTemplateSerializer.Serialize(server);

        Assert.Single(server.Tags);
        Assert.Equal("keep-this", server.Tags[0]);
        Assert.Single(server.Labels);
        Assert.Equal("keep these notes", server.Notes);
        Assert.Single(server.RunsOn);
    }

    [Fact]
    public void serialize__with_template_name__overrides_resource_name()
    {
        var server = new Server
        {
            Name = "my-personal-server",
            Kind = "Server",
            Cpus = [new Cpu { Model = "Intel i5-1240P", Cores = 12, Threads = 16 }]
        };

        var yaml = ResourceTemplateSerializer.Serialize(server, templateName: "Official-Model-X");

        Assert.Contains("name: Official-Model-X", yaml);
        Assert.DoesNotContain("my-personal-server", yaml);
        Assert.Contains("kind: Server", yaml);
        Assert.Contains("model: Intel i5-1240P", yaml);
    }
}
