using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.Discovery;

/// <summary>Renders discovered resources as a RackPeek YAML document.</summary>
public static class DiscoveryDocument {
    public static string ToYaml(IEnumerable<Resource> resources) {
        return YamlResourceCollection.SerializeRootAsync(new YamlRoot {
            Version = RackPeekConfigMigrationDeserializer.ListOfMigrations.Count,
            Resources = resources.ToList(),
            Connections = []
        });
    }
}
