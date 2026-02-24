using System.Collections.Specialized;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.UpsUnits;
using DocMigrator.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Domain.Persistence.Yaml;

public class ResourceCollection
{
    public readonly SemaphoreSlim FileLock = new(1, 1);
    public List<Resource> Resources { get; } = new();
}

public class RackPeekConfigMigrationDeserializer : YamlMigrationDeserializer<YamlRoot>
{
    public RackPeekConfigMigrationDeserializer(IServiceProvider serviceProvider,
            ILogger<YamlMigrationDeserializer<YamlRoot>> logger,
            DeserializerBuilder deserializerBuilder,
            SerializerBuilder serializerBuilder) :
        base(serviceProvider, logger, new List<Func<IServiceProvider, Dictionary<object,object>, ValueTask>>{
                // List migrations here
                EnsureSchemaVersionExists,
            },
            deserializerBuilder, serializerBuilder) {}

    #region Migrations

    // Define migration functions here

    public static ValueTask EnsureSchemaVersionExists(IServiceProvider serviceProvider, Dictionary<object, object> obj)
    {
        if (!obj.ContainsKey("schemaVersion"))
        {
            obj["schemaVersion"] = 0;
            if (obj.ContainsKey("version"))
            {
                obj["schemaVersion"] = obj["version"];
            }
        }

        obj.Remove("version");
        return ValueTask.CompletedTask;
    }

    #endregion
}

public sealed class YamlResourceCollection(
    string filePath,
    ITextFileStore fileStore,
    ResourceCollection resourceCollection,
    RackPeekConfigMigrationDeserializer _deserializer)
    : IResourceCollection
{
    // Bump this when your YAML schema changes, and add a migration step below.
    private const int CurrentSchemaVersion = 1;

    public Task<bool> Exists(string name)
    {
        return Task.FromResult(resourceCollection.Resources.Exists(r =>
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<Dictionary<string, int>> GetTagsAsync()
    {
        var result = resourceCollection.Resources
            .SelectMany(r => r.Tags) // flatten all tag arrays
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<T>> GetAllOfTypeAsync<T>()
    {
        return Task.FromResult<IReadOnlyList<T>>(resourceCollection.Resources.OfType<T>().ToList());
    }

    public Task<IReadOnlyList<Resource>> GetDependantsAsync(string name)
    {
        return Task.FromResult<IReadOnlyList<Resource>>(resourceCollection.Resources
            .Where(r => r.RunsOn?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false)
            .ToList());
    }

    public Task<IReadOnlyList<Resource>> GetByTagAsync(string name)
    {
        return Task.FromResult<IReadOnlyList<Resource>>(
            resourceCollection.Resources
                .Where(r => r.Tags.Contains(name))
                .ToList()
        );
    }

    public IReadOnlyList<Hardware> HardwareResources =>
        resourceCollection.Resources.OfType<Hardware>().ToList();

    public IReadOnlyList<SystemResource> SystemResources =>
        resourceCollection.Resources.OfType<SystemResource>().ToList();

    public IReadOnlyList<Service> ServiceResources =>
        resourceCollection.Resources.OfType<Service>().ToList();

    public Task<Resource?> GetByNameAsync(string name)
    {
        return Task.FromResult(resourceCollection.Resources.FirstOrDefault(r =>
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<T?> GetByNameAsync<T>(string name) where T : Resource
    {
        var resource =
            resourceCollection.Resources.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(resource as T);
    }

    public Resource? GetByName(string name)
    {
        return resourceCollection.Resources.FirstOrDefault(r =>
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task LoadAsync()
    {
        // Read raw YAML so we can back it up exactly before any migration writes.
        var yaml = await fileStore.ReadAllTextAsync(filePath);
        if (string.IsNullOrWhiteSpace(yaml))
        {
            resourceCollection.Resources.Clear();
            return;
        }

        var root = DeserializeRoot(yaml);
        if (root == null)
        {
            // Keep behavior aligned with your previous code: if YAML is invalid, treat as empty.
            resourceCollection.Resources.Clear();
            return;
        }

        // Guard: config is newer than this app understands.
        if (root.SchemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Config schema version {root.SchemaVersion} is newer than this application supports ({CurrentSchemaVersion}).");
        }

        // If older, backup first, then migrate step-by-step, then save.
        if (root.SchemaVersion < CurrentSchemaVersion)
        {
            await BackupOriginalAsync(yaml);

            root = await _deserializer.Deserialize(yaml);
            // root = await MigrateAsync(root);

            // Ensure we persist the migrated root (with updated version)
            await SaveRootAsync(root);
        }

        resourceCollection.Resources.Clear();
        resourceCollection.Resources.AddRange(root.Resources ?? []);
    }

    public Task AddAsync(Resource resource)
    {
        return UpdateWithLockAsync(list =>
        {
            if (list.Any(r => r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"'{resource.Name}' already exists.");

            resource.Kind = GetKind(resource);
            list.Add(resource);
        });
    }

    public Task UpdateAsync(Resource resource)
    {
        return UpdateWithLockAsync(list =>
        {
            var index = list.FindIndex(r => r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));
            if (index == -1) throw new InvalidOperationException("Not found.");

            resource.Kind = GetKind(resource);
            list[index] = resource;
        });
    }

    public Task DeleteAsync(string name)
    {
        return UpdateWithLockAsync(list =>
            list.RemoveAll(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task UpdateWithLockAsync(Action<List<Resource>> action)
    {
        await resourceCollection.FileLock.WaitAsync();
        try
        {
            action(resourceCollection.Resources);

            // Always write current schema version when app writes the file.
            var root = new YamlRoot
            {
                SchemaVersion = CurrentSchemaVersion,
                Resources = resourceCollection.Resources
            };

            await SaveRootAsync(root);
        }
        finally
        {
            resourceCollection.FileLock.Release();
        }
    }

    // ----------------------------
    // Versioning + migration
    // ----------------------------

    private async Task BackupOriginalAsync(string originalYaml)
    {
        // Timestamped backup for safe rollback
        var backupPath = $"{filePath}.bak.{DateTime.UtcNow:yyyyMMddHHmmss}";
        await fileStore.WriteAllTextAsync(backupPath, originalYaml);
    }

    private Task<YamlRoot> MigrateAsync(YamlRoot root)
    {
        // Step-by-step migrations until we reach CurrentSchemaVersion
        while (root.SchemaVersion < CurrentSchemaVersion)
        {
            root = root.SchemaVersion switch
            {
                0 => MigrateV0ToV1(root),
                _ => throw new InvalidOperationException(
                    $"No migration is defined from version {root.SchemaVersion} to {root.SchemaVersion + 1}.")
            };
        }

        return Task.FromResult(root);
    }

    private YamlRoot MigrateV0ToV1(YamlRoot root)
    {
        // V0 -> V1 example migration:
        // - Ensure 'kind' is normalized on all resources
        // - Ensure tags collections aren’t null
        if (root.Resources != null)
        {
            foreach (var r in root.Resources)
            {
                r.Kind = GetKind(r);
                r.Tags ??= [];
            }
        }

        root.SchemaVersion = 1;
        return root;
    }

    // ----------------------------
    // YAML read/write
    // ----------------------------

    private YamlRoot? DeserializeRoot(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithCaseInsensitivePropertyMatching()
            .WithTypeConverter(new StorageSizeYamlConverter())
            .WithTypeConverter(new NotesStringYamlConverter())
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

            // var rootDeserializer = Setup();
            // var rootDeserializer = new YamlRootDeserializer();

        try
        {
            // If 'version' is missing, int defaults to 0 => treated as V0.
            var root = deserializer.Deserialize<YamlRoot>(yaml);

            // If YAML had only "resources:" previously, this will still work.
            root ??= new YamlRoot { SchemaVersion = 0, Resources = new List<Resource>() };
            root.Resources ??= new List<Resource>();

            return root;
        }
        catch (YamlException)
        {
            return null;
        }
    }

    private async Task SaveRootAsync(YamlRoot root)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new StorageSizeYamlConverter())
            .WithTypeConverter(new NotesStringYamlConverter())
            .ConfigureDefaultValuesHandling(
                DefaultValuesHandling.OmitNull |
                DefaultValuesHandling.OmitEmptyCollections
            )
            .Build();

        // Preserve ordering: version first, then resources
        var payload = new OrderedDictionary
        {
            ["schemaVersion"] = root.SchemaVersion,
            ["resources"] = (root.Resources ?? new List<Resource>()).Select(SerializeResource).ToList()
        };

        await fileStore.WriteAllTextAsync(filePath, serializer.Serialize(payload));
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
            .WithTypeConverter(new NotesStringYamlConverter())
            .ConfigureDefaultValuesHandling(
                DefaultValuesHandling.OmitNull |
                DefaultValuesHandling.OmitEmptyCollections
            )
            .Build();

        var yaml = serializer.Serialize(resource);

        var props = new DeserializerBuilder()
            .Build()
            .Deserialize<Dictionary<string, object?>>(yaml);

        foreach (var (key, value) in props)
            if (!string.Equals(key, "kind", StringComparison.OrdinalIgnoreCase))
                map[key] = value;

        return map;
    }

}

public class YamlRoot
{
    public int SchemaVersion { get; set; } // <- NEW: YAML schema version
    public List<Resource>? Resources { get; set; }
}
