using RackPeek.Domain.Resources.Services;
using RackPeek.Yaml;

namespace Tests.Yaml;

public class ServiceDeserializationTests
{
    public static IServiceRepository CreateSut(string yaml)
    {
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "RackPeekTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, "config.yaml");
        File.WriteAllText(filePath, yaml);

        var yamlResourceCollection = new YamlResourceCollection(filePath);
        return new YamlServiceRepository(yamlResourceCollection);
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

        var sut = CreateSut(yaml);

        // When
        var resources = await sut.GetAllAsync();

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