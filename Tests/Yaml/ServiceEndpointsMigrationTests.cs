using RackPeek.Domain.Persistence.Yaml;

namespace Tests.Yaml;

public class ServiceEndpointsMigrationTests {
    [Fact]
    public async Task V4_service_network_is_preserved_and_copied_to_endpoints() {
        var network = new Dictionary<object, object> {
            ["ip"] = "192.168.1.20",
            ["port"] = 8096,
            ["protocol"] = "TCP"
        };
        var service = new Dictionary<object, object> {
            ["kind"] = "Service",
            ["name"] = "media",
            ["network"] = network
        };
        var root = new Dictionary<object, object> {
            ["version"] = 4,
            ["resources"] = new List<object> { service }
        };

        await RackPeekConfigMigrationDeserializer.AddServiceEndpointsV5(
            null!, root);

        Assert.Equal(5, root["version"]);
        Assert.Same(network, service["network"]);
        List<object> endpoints = Assert.IsType<List<object>>(service["endpoints"]);
        Assert.Equal(network, Assert.IsType<Dictionary<object, object>>(Assert.Single(endpoints)));
        Assert.NotSame(network, endpoints[0]);
    }
}
