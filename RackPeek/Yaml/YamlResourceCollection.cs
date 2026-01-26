using System.Collections.Specialized;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.SystemResources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Yaml;

public sealed class YamlResourceCollection
{
    private readonly List<ResourceEntry> _entries = [];
    private readonly List<string> _knownFiles = [];

    public IReadOnlyList<string> SourceFiles => _knownFiles.ToList();

    public IReadOnlyList<Hardware> HardwareResources =>
        _entries.Select(e => e.Resource).OfType<Hardware>().ToList();

    public IReadOnlyList<SystemResource> SystemResources =>
        _entries.Select(e => e.Resource).OfType<SystemResource>().ToList();

    public void LoadFiles(IEnumerable<string> filePaths)
    {
        foreach (var file in filePaths)
        {
            // Track the file even if it is empty
            if (!_knownFiles.Contains(file))
                _knownFiles.Add(file);

            var yaml = File.ReadAllText(file);
            var resources = Deserialize(yaml);

            foreach (var resource in resources)
            {
                _entries.Add(new ResourceEntry(resource, file));
            }
        }
    }

    public void Load(string yaml, string file)
    {
        if (!_knownFiles.Contains(file))
            _knownFiles.Add(file);

        foreach (var resource in Deserialize(yaml))
            _entries.Add(new ResourceEntry(resource, file));
    }

    public void SaveAll()
    {
        foreach (var file in _knownFiles)
        {
            var resources = _entries
                .Where(e => e.SourceFile == file)
                .Select(e => e.Resource);

            SaveToFile(file, resources);
        }
    }

    // ----------------------------
    // CRUD operations
    // ----------------------------

    public void Add(Resource resource, string sourceFile)
    {
        _entries.Add(new ResourceEntry(resource, sourceFile));
    }

    public void Update(Resource resource)
    {
        var existing = _entries.FirstOrDefault(e =>
            e.Resource.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Resource '{resource.Name}' not found.");

        // keep file ownership
        _entries.Remove(existing);
        _entries.Add(new ResourceEntry(resource, existing.SourceFile));
    }

    public void Delete(string name)
    {
        var existing = _entries.FirstOrDefault(e =>
            e.Resource.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
            throw new InvalidOperationException($"Resource '{name}' not found.");

        _entries.Remove(existing);
    }

    public Resource? GetByName(string name)
    {
        return _entries
            .Select(e => e.Resource)
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    // ----------------------------
    // Serialization helpers
    // ----------------------------

    private static void SaveToFile(string filePath, IEnumerable<Resource> resources)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var payload = new OrderedDictionary
        {
            ["resources"] = resources
                .Select(SerializeResource)
                .ToList()
        };

        File.WriteAllText(filePath, serializer.Serialize(payload));
    }

    private static OrderedDictionary SerializeResource(Resource resource)
    {
        var map = new OrderedDictionary
        {
            ["kind"] = resource switch
            {
                Server => "Server",
                Switch => "Switch",
                Firewall => "Firewall",
                Router => "Router",
                Desktop => "Desktop",
                Laptop => "Laptop",
                AccessPoint => "AccessPoint",
                Ups => "Ups",
                SystemResource => "System",
                _ => throw new InvalidOperationException($"Unknown resource type: {resource.GetType().Name}")
            }
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(resource);

        var props = new DeserializerBuilder()
            .Build()
            .Deserialize<Dictionary<string, object?>>(yaml);

        foreach (var (key, value) in props)
        {
            if (key == "kind") continue;
            map[key] = value;
        }

        return map;
    }

    private static List<Resource> Deserialize(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return new List<Resource>();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithCaseInsensitivePropertyMatching()
            .WithTypeConverter(new StorageSizeYamlConverter())
            .Build();

        var raw = deserializer.Deserialize<
            Dictionary<string, List<Dictionary<string, object>>>>(yaml);

        if (raw == null || !raw.TryGetValue("resources", out var items))
            return new List<Resource>();

        var resources = new List<Resource>();

        foreach (var item in items)
        {
            var kind = item["kind"].ToString();
            var typedYaml = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()
                .Serialize(item);

            Resource resource = kind switch
            {
                "Server" => deserializer.Deserialize<Server>(typedYaml),
                "Switch" => deserializer.Deserialize<Switch>(typedYaml),
                "Firewall" => deserializer.Deserialize<Firewall>(typedYaml),
                "Router" => deserializer.Deserialize<Router>(typedYaml),
                "Desktop" => deserializer.Deserialize<Desktop>(typedYaml),
                "Laptop" => deserializer.Deserialize<Laptop>(typedYaml),
                "AccessPoint" => deserializer.Deserialize<AccessPoint>(typedYaml),
                "Ups" => deserializer.Deserialize<Ups>(typedYaml),
                "System" => deserializer.Deserialize<SystemResource>(typedYaml),
                _ => throw new InvalidOperationException($"Unknown kind: {kind}")
            };

            resources.Add(resource);
        }

        return resources;
    }

    private sealed record ResourceEntry(Resource Resource, string SourceFile);
}
