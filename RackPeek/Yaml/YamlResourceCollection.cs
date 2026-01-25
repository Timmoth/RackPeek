using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek;

public class YamlResourceCollection
{
    private readonly List<Resource> _resources = new();
    
    public IReadOnlyList<Hardware> HardwareResources => _resources.OfType<Hardware>().ToList();
    public IReadOnlyList<SystemResource> SystemResources => _resources.OfType<SystemResource>().ToList();

    public void Load(List<string> yamlContents)
    {
        foreach (var yamlContent in yamlContents)
        {
            _resources.AddRange(Deserialize(yamlContent));
        }
    }
    
    private static List<Resource> Deserialize(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithCaseInsensitivePropertyMatching()
            .WithTypeConverter(new StorageSizeYamlConverter())
            .Build();

        var raw = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(yaml);

        var resources = new List<Resource>();

        foreach (var item in raw["resources"])
        {
            var kind = item["kind"].ToString();

            var typedYaml = new SerializerBuilder().Build().Serialize(item);
    
            Resource resource = kind switch
            {
                // Hardware
                "Server" => deserializer.Deserialize<Server>(typedYaml),
                "Switch" => deserializer.Deserialize<Switch>(typedYaml),
                "Firewall" => deserializer.Deserialize<Firewall>(typedYaml),
                "Router" => deserializer.Deserialize<Router>(typedYaml),
                "Desktop" => deserializer.Deserialize<Desktop>(typedYaml),
                "Laptop" => deserializer.Deserialize<Laptop>(typedYaml),
                "AccessPoint" => deserializer.Deserialize<AccessPoint>(typedYaml),
                "Ups" => deserializer.Deserialize<Ups>(typedYaml),
                
                // System
                "System" => deserializer.Deserialize<SystemResource>(typedYaml),
                _ => throw new InvalidOperationException($"Unknown kind: {kind}")
            };

            resources.Add(resource);
        }

        return resources;
    }
    
}