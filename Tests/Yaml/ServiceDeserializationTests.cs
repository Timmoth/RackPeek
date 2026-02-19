using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources.Services;

namespace Tests.Yaml;

public class ServiceDeserializationTests
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
    public async Task deserialize_yaml_kind_Service()
    {
        // Given
        var yaml = @"
resources:
  - kind: Service
    name: immich
    network:
      ip: 192.168.0.4
      port: 8080
      protocol: TCP
      url: http://immich.lan:8080
    runsOn: proxmox-host
";

        var sut = await CreateSut(yaml);

        // When
        var resources = await sut.GetAllOfTypeAsync<Service>();

        // Then
        var resource = Assert.Single(resources);
        var service = Assert.IsType<Service>(resource);

        Assert.Equal("immich", service.Name);
        Assert.Equal("Service", service.Kind);
        Assert.Equal("proxmox-host", service.RunsOn);

        Assert.NotNull(service.Network);
        Assert.Equal("192.168.0.4", service.Network.Ip);
        Assert.Equal(8080, service.Network.Port);
        Assert.Equal("TCP", service.Network.Protocol);
        Assert.Equal("http://immich.lan:8080", service.Network.Url);
    }
}