using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Yaml;

public class SystemDeserializationTests
{
    public static async Task<IResourceCollection> CreateSut(string yaml)
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "RackPeekTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, "config.yaml"); 
        await File.WriteAllTextAsync(filePath, yaml);

        var yamlResourceCollection = new YamlResourceCollection(filePath, new PhysicalTextFileStore(), new ResourceCollection());
        await yamlResourceCollection.LoadAsync();


        return yamlResourceCollection;
    }

    [Fact]
    public async Task deserialize_yaml_kind_System()
    {
        // type: Hypervisor | Baremetal | VM | Container 

        // Given
        var yaml = @"
resources:
  - kind: System
    type: Hypervisor
    name: home-virtualization-host
    os: proxmox     
    cores: 2
    ram: 12gb
    drives:
        - size: 2Tb
        - size: 1tb   
    runsOn: dell-c6400-node-01
";
        var sut = await CreateSut(yaml);

        // When
        var resources = await sut.GetAllOfTypeAsync<SystemResource>();

        // Then
        var resource = Assert.Single(resources);
        Assert.IsType<SystemResource>(resource);
        var system = resource;
        Assert.NotNull(system);
        Assert.Equal("Hypervisor", system.Type);
        Assert.Equal("home-virtualization-host", system.Name);
        Assert.Equal("proxmox", system.Os);
        Assert.Equal(2, system.Cores);
        Assert.Equal(12, system.Ram);

        // Drives
        Assert.NotNull(system.Drives);
        Assert.Equal(2048, system.Drives[0].Size);
        Assert.Equal(1024, system.Drives[1].Size);

        Assert.Equal("dell-c6400-node-01", system.RunsOn);
    }
}