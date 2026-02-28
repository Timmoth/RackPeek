using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Api;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Api;

public class UpsertInventoryUseCaseTests
{
    private readonly InMemoryResourceCollection _repo = new();
    private readonly UpsertInventoryUseCase _sut;

    public UpsertInventoryUseCaseTests()
    {
        _sut = new UpsertInventoryUseCase(_repo);
    }

    [Fact]
    public async Task Creates_server_when_none_exists()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            RamGb = 128,
            RamMts = 3200,
            Ipmi = true
        };

        var result = await _sut.ExecuteAsync(request);

        Assert.Equal("srv01", result.Hardware.Name);
        Assert.Equal("Server", result.Hardware.Kind);
        Assert.Equal("created", result.Hardware.Action);
        Assert.Null(result.System);

        var server = await _repo.GetByNameAsync<Server>("srv01");
        Assert.NotNull(server);
        Assert.Equal(128, server.Ram?.Size);
        Assert.Equal(3200, server.Ram?.Mts);
        Assert.True(server.Ipmi);
    }

    [Fact]
    public async Task Updates_existing_server_on_second_call()
    {
        var request1 = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            RamGb = 64
        };

        await _sut.ExecuteAsync(request1);

        var request2 = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            RamGb = 128
        };

        var result = await _sut.ExecuteAsync(request2);

        Assert.Equal("updated", result.Hardware.Action);

        var server = await _repo.GetByNameAsync<Server>("srv01");
        Assert.NotNull(server);
        Assert.Equal(128, server.Ram?.Size);
    }

    [Fact]
    public async Task Creates_system_resource_when_os_is_provided()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Os = "Ubuntu 24.04",
            SystemType = "baremetal"
        };

        var result = await _sut.ExecuteAsync(request);

        Assert.NotNull(result.System);
        Assert.Equal("srv01-system", result.System.Name);
        Assert.Equal("System", result.System.Kind);
        Assert.Equal("created", result.System.Action);

        var system = await _repo.GetByNameAsync<SystemResource>("srv01-system");
        Assert.NotNull(system);
        Assert.Equal("Ubuntu 24.04", system.Os);
        Assert.Equal("baremetal", system.Type);
        Assert.Contains("srv01", system.RunsOn);
    }

    [Fact]
    public async Task Does_not_create_system_when_no_system_fields()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            RamGb = 64
        };

        var result = await _sut.ExecuteAsync(request);

        Assert.Null(result.System);

        var systems = await _repo.GetAllOfTypeAsync<SystemResource>();
        Assert.Empty(systems);
    }

    [Fact]
    public async Task Uses_custom_system_name_when_provided()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            SystemName = "srv01-os",
            Os = "Proxmox 8.0",
            SystemType = "hypervisor"
        };

        var result = await _sut.ExecuteAsync(request);

        Assert.NotNull(result.System);
        Assert.Equal("srv01-os", result.System.Name);

        var system = await _repo.GetByNameAsync<SystemResource>("srv01-os");
        Assert.NotNull(system);
        Assert.Contains("srv01", system.RunsOn);
    }

    [Fact]
    public async Task Throws_on_empty_hostname()
    {
        var request = new InventoryRequest
        {
            Hostname = "  ",
            HardwareType = "server"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.ExecuteAsync(request));
    }

    [Fact]
    public async Task Throws_on_invalid_system_type()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Os = "Ubuntu",
            SystemType = "invalidtype"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.ExecuteAsync(request));
    }

    [Fact]
    public async Task Throws_on_invalid_hardware_type()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "router"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.ExecuteAsync(request));
    }

    [Fact]
    public async Task Creates_desktop_with_model()
    {
        var request = new InventoryRequest
        {
            Hostname = "desk01",
            HardwareType = "desktop",
            Model = "Dell OptiPlex 7090",
            RamGb = 32
        };

        var result = await _sut.ExecuteAsync(request);

        Assert.Equal("Desktop", result.Hardware.Kind);

        var desktop = await _repo.GetByNameAsync<Desktop>("desk01");
        Assert.NotNull(desktop);
        Assert.Equal("Dell OptiPlex 7090", desktop.Model);
        Assert.Equal(32, desktop.Ram?.Size);
    }

    [Fact]
    public async Task Desktop_throws_when_model_missing()
    {
        var request = new InventoryRequest
        {
            Hostname = "desk01",
            HardwareType = "desktop"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.ExecuteAsync(request));
    }

    [Fact]
    public async Task Creates_laptop()
    {
        var request = new InventoryRequest
        {
            Hostname = "laptop01",
            HardwareType = "laptop",
            Model = "ThinkPad X1 Carbon",
            RamGb = 16
        };

        var result = await _sut.ExecuteAsync(request);

        Assert.Equal("Laptop", result.Hardware.Kind);

        var laptop = await _repo.GetByNameAsync<Laptop>("laptop01");
        Assert.NotNull(laptop);
        Assert.Equal("ThinkPad X1 Carbon", laptop.Model);
    }

    [Fact]
    public async Task Maps_cpus_correctly()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Cpus =
            [
                new InventoryCpu { Model = "AMD EPYC 7302P", Cores = 16, Threads = 32 },
                new InventoryCpu { Model = "AMD EPYC 7302P", Cores = 16, Threads = 32 }
            ]
        };

        await _sut.ExecuteAsync(request);

        var server = await _repo.GetByNameAsync<Server>("srv01");
        Assert.NotNull(server?.Cpus);
        Assert.Equal(2, server.Cpus.Count);
        Assert.Equal("AMD EPYC 7302P", server.Cpus[0].Model);
        Assert.Equal(16, server.Cpus[0].Cores);
        Assert.Equal(32, server.Cpus[0].Threads);
    }

    [Fact]
    public async Task Maps_drives_correctly()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Drives =
            [
                new InventoryDrive { Type = "nvme", Size = 1024 },
                new InventoryDrive { Type = "ssd", Size = 512 }
            ]
        };

        await _sut.ExecuteAsync(request);

        var server = await _repo.GetByNameAsync<Server>("srv01");
        Assert.NotNull(server?.Drives);
        Assert.Equal(2, server.Drives.Count);
        Assert.Equal("nvme", server.Drives[0].Type);
        Assert.Equal(1024, server.Drives[0].Size);
    }

    [Fact]
    public async Task Maps_gpus_correctly()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Gpus = [new InventoryGpu { Model = "NVIDIA RTX 3090", Vram = 24 }]
        };

        await _sut.ExecuteAsync(request);

        var server = await _repo.GetByNameAsync<Server>("srv01");
        Assert.NotNull(server?.Gpus);
        Assert.Single(server.Gpus);
        Assert.Equal("NVIDIA RTX 3090", server.Gpus[0].Model);
        Assert.Equal(24, server.Gpus[0].Vram);
    }

    [Fact]
    public async Task Maps_nics_correctly()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Nics = [new InventoryNic { Type = "rj45", Speed = 10.0, Ports = 2 }]
        };

        await _sut.ExecuteAsync(request);

        var server = await _repo.GetByNameAsync<Server>("srv01");
        Assert.NotNull(server?.Nics);
        Assert.Single(server.Nics);
        Assert.Equal("rj45", server.Nics[0].Type);
        Assert.Equal(10.0, server.Nics[0].Speed);
        Assert.Equal(2, server.Nics[0].Ports);
    }

    [Fact]
    public async Task Throws_on_invalid_drive_type()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Drives = [new InventoryDrive { Type = "floppy", Size = 1 }]
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.ExecuteAsync(request));
    }

    [Fact]
    public async Task Throws_on_invalid_nic_type()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Nics = [new InventoryNic { Type = "invalid", Speed = 1.0 }]
        };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.ExecuteAsync(request));
    }

    [Fact]
    public async Task Upsert_replaces_sub_resources_on_update()
    {
        var request1 = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Cpus = [new InventoryCpu { Model = "Old CPU", Cores = 4 }]
        };

        await _sut.ExecuteAsync(request1);

        var request2 = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Cpus =
            [
                new InventoryCpu { Model = "New CPU 1", Cores = 8 },
                new InventoryCpu { Model = "New CPU 2", Cores = 8 }
            ]
        };

        await _sut.ExecuteAsync(request2);

        var server = await _repo.GetByNameAsync<Server>("srv01");
        Assert.NotNull(server?.Cpus);
        Assert.Equal(2, server.Cpus.Count);
        Assert.Equal("New CPU 1", server.Cpus[0].Model);
    }

    [Fact]
    public async Task Preserves_tags_and_labels()
    {
        var request = new InventoryRequest
        {
            Hostname = "srv01",
            HardwareType = "server",
            Tags = ["production", "compute"],
            Labels = new Dictionary<string, string>
            {
                ["env"] = "prod",
                ["site"] = "primary"
            }
        };

        await _sut.ExecuteAsync(request);

        var server = await _repo.GetByNameAsync<Server>("srv01");
        Assert.NotNull(server);
        Assert.Contains("production", server.Tags);
        Assert.Contains("compute", server.Tags);
        Assert.Equal("prod", server.Labels["env"]);
        Assert.Equal("primary", server.Labels["site"]);
    }
}
