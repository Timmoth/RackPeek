using System.Collections.Specialized;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Yaml;

public sealed class YamlResourceCollection(string filePath)
{
    private readonly Lock _fileLock = new();
    private readonly List<Resource> _resources = LoadFromFile(filePath);

    public IReadOnlyList<Hardware> HardwareResources =>
        _resources.OfType<Hardware>().ToList();

    public IReadOnlyList<SystemResource> SystemResources =>
        _resources.OfType<SystemResource>().ToList();

    public IReadOnlyList<Service> ServiceResources =>
        _resources.OfType<Service>().ToList();

    // --- CRUD ---

    public void Add(Resource resource)
    {
        UpdateWithLock(list =>
        {
            if (list.Any(r => r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"'{resource.Name}' already exists.");

            resource.Kind = GetKind(resource);
            list.Add(resource);
        });
    }

    public void Update(Resource resource)
    {
        UpdateWithLock(list =>
        {
            var index = list.FindIndex(r => r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));
            if (index == -1) throw new InvalidOperationException("Not found.");
            list[index] = resource;
        });
    }

    public void Delete(string name)
    {
        UpdateWithLock(list => { list.RemoveAll(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)); });
    }
    
    
    private void UpdateWithLock(Action<List<Resource>> action)
    {
        lock (_fileLock)
        {
            action(_resources);
            
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var payload = new OrderedDictionary
            {
                ["resources"] = _resources.Select(SerializeResource).ToList()
            };

            File.WriteAllText(filePath, serializer.Serialize(payload));
            
        }
    }

    private string GetKind(Resource resource)
    {
        return resource switch
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
            Service => "Service",
            _ => throw new InvalidOperationException($"Unknown resource type: {resource.GetType().Name}")
        };
    }

    private OrderedDictionary SerializeResource(Resource resource)
    {
        var map = new OrderedDictionary
        {
            ["kind"] = GetKind(resource)
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(resource);

        var props = new DeserializerBuilder()
            .Build()
            .Deserialize<Dictionary<string, object?>>(yaml);

        foreach (var (key, value) in props)
            if (key != "kind")
                map[key] = value;

        return map;
    }
    
    private static List<Resource> LoadFromFile(string filePath)
    {
        // 1. Robustness: Handle missing or empty files immediately
        if (!File.Exists(filePath)) return new List<Resource>();
        var yaml = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(yaml)) return new List<Resource>();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithCaseInsensitivePropertyMatching()
            .WithTypeConverter(new StorageSizeYamlConverter())
            // 2. The "Pragmatic" Fix: Automatically choose the class based on the "kind" key
            .WithTypeDiscriminatingNodeDeserializer(options =>
            {
                options.AddKeyValueTypeDiscriminator<Resource>("kind", new Dictionary<string, Type>
                {
                    { Server.KindLabel, typeof(Server) },
                    { Switch.KindLabel, typeof(Switch) },
                    { Firewall.KindLabel, typeof(Firewall) },
                    { Router.KindLabel, typeof(Router) },
                    { Desktop.KindLabel, typeof(Desktop) },
                    { Laptop.KindLabel, typeof(Laptop) },
                    { AccessPoint.KindLabel, typeof(AccessPoint) },
                    { Ups.KindLabel, typeof(Ups) },
                    { SystemResource.KindLabel, typeof(SystemResource) },
                    { Service.KindLabel, typeof(Service) }
                });
            })
            .Build();

        try
        {
            // 3. Deserialize into a wrapper class to handle the "resources:" root key
            var root = deserializer.Deserialize<YamlRoot>(yaml);
            return root?.Resources ?? new List<Resource>();
        }
        catch (YamlException)
        {
            // Handle malformed YAML here or return empty list
            return new List<Resource>();
        }
    }

    // Simple wrapper class to match the YAML structure
    private class YamlRoot
    {
        public List<Resource>? Resources { get; set; }
    }

    public Resource? GetByName(string name)
    {
        return _resources.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}