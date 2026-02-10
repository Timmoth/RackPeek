using System.Collections.Specialized;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class ResourceCollection
{
    public List<Resource> Resources { get; } = new();
    public readonly SemaphoreSlim FileLock = new(1, 1);

}
public sealed class YamlResourceCollection(
    string filePath,
    ITextFileStore fileStore,
    ResourceCollection resourceCollection)
    : IResourceCollection
{
    public async Task LoadAsync()
    {
        var loaded = await LoadFromFileAsync();
        resourceCollection.Resources.Clear();
        resourceCollection.Resources.AddRange(loaded);
    }

    public IReadOnlyList<Hardware> HardwareResources =>
        resourceCollection.Resources.OfType<Hardware>().ToList();

    public IReadOnlyList<SystemResource> SystemResources =>
        resourceCollection.Resources.OfType<SystemResource>().ToList();

    public IReadOnlyList<Service> ServiceResources =>
        resourceCollection.Resources.OfType<Service>().ToList();

    public Resource? GetByName(string name) =>
        resourceCollection.Resources.FirstOrDefault(r =>
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public Task AddAsync(Resource resource) =>
        UpdateWithLockAsync(list =>
        {
            if (list.Any(r => r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"'{resource.Name}' already exists.");

            resource.Kind = GetKind(resource);
            list.Add(resource);
        });

    public Task UpdateAsync(Resource resource) =>
        UpdateWithLockAsync(list =>
        {
            var index = list.FindIndex(r => r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));
            if (index == -1) throw new InvalidOperationException("Not found.");

            resource.Kind = GetKind(resource);
            list[index] = resource;
        });

    public Task DeleteAsync(string name) =>
        UpdateWithLockAsync(list =>
            list.RemoveAll(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

    private async Task UpdateWithLockAsync(Action<List<Resource>> action)
    {
        await resourceCollection.FileLock.WaitAsync();
        try
        {
            action(resourceCollection.Resources);

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var payload = new OrderedDictionary
            {
                ["resources"] = resourceCollection.Resources.Select(SerializeResource).ToList()
            };

            await fileStore.WriteAllTextAsync(
                filePath,
                serializer.Serialize(payload));
        }
        finally
        {
            resourceCollection.FileLock.Release();
        }
    }

    private async Task<List<Resource>> LoadFromFileAsync()
    {
        var yaml = await fileStore.ReadAllTextAsync(filePath);
        if (string.IsNullOrWhiteSpace(yaml))
            return new();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithCaseInsensitivePropertyMatching()
            .WithTypeConverter(new StorageSizeYamlConverter())
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
            var root = deserializer.Deserialize<YamlRoot>(yaml);
            return root?.Resources ?? new();
        }
        catch (YamlException)
        {
            return new();
        }
    }

    private string GetKind(Resource resource) => resource switch
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


}
public class YamlRoot
{
    public List<Resource>? Resources { get; set; }
}