using System.Collections.Specialized;
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
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Domain.Persistence.Yaml;

public class ResourceCollection
{
    public readonly SemaphoreSlim FileLock = new(1, 1);
    public List<Resource> Resources { get; } = new();
}

public sealed class YamlResourceCollection(
    string filePath,
    ITextFileStore fileStore,
    ResourceCollection resourceCollection,
    RackPeekConfigMigrationDeserializer _deserializer)
    : IResourceCollection
{
    // Bump this when your YAML schema changes, and add a migration step below.
    private static readonly int CurrentSchemaVersion = RackPeekConfigMigrationDeserializer.ListOfMigrations.Count;

    public Task<bool> Exists(string name)
    {
        return Task.FromResult(resourceCollection.Resources.Exists(r =>
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<string?> GetKind(string? name)
    {
        return Task.FromResult(resourceCollection.Resources.FirstOrDefault(r =>
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Kind);
        
    }
    public Task<IReadOnlyList<(Resource, string)>> GetByLabelAsync(string name)
    {
        var result = resourceCollection.Resources
            .Where(r => r.Labels != null && r.Labels.TryGetValue(name, out _))
            .Select(r => (r, r.Labels![name]))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<(Resource, string)>>(result);
    }
    public Task<Dictionary<string, int>> GetLabelsAsync()
    {
        var result = resourceCollection.Resources
            .SelectMany(r => r.Labels ?? Enumerable.Empty<KeyValuePair<string, string>>())
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .GroupBy(kvp => kvp.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(result);
    }
    public Task<IReadOnlyList<(Resource, string)>> GetResourceIpsAsync()
    {
        var result = new List<(Resource, string)>();

        var allResources = resourceCollection.Resources;

        // Build fast lookup for systems
        var systemsByName = allResources
            .OfType<SystemResource>()
            .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        // Cache resolved system IPs (prevents repeated recursion)
        var resolvedSystemIps = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var resource in allResources)
        {
            switch (resource)
            {
                case SystemResource system:
                {
                    var ip = ResolveSystemIp(system, systemsByName, resolvedSystemIps);
                    if (!string.IsNullOrWhiteSpace(ip))
                        result.Add((system, ip));
                    break;
                }

                case Service service:
                {
                    var ip = ResolveServiceIp(service, systemsByName, resolvedSystemIps);
                    if (!string.IsNullOrWhiteSpace(ip))
                        result.Add((service, ip));
                    break;
                }
            }
        }

        return Task.FromResult((IReadOnlyList<(Resource, string)>)result);
    }
    private string? ResolveSystemIp(
        SystemResource system,
        Dictionary<string, SystemResource> systemsByName,
        Dictionary<string, string?> cache)
    {
        // Return cached result if already resolved
        if (cache.TryGetValue(system.Name, out var cached))
            return cached;

        // Direct IP wins
        if (!string.IsNullOrWhiteSpace(system.Ip))
        {
            cache[system.Name] = system.Ip;
            return system.Ip;
        }

        // Must have exactly one parent
        if (system.RunsOn?.Count != 1)
        {
            cache[system.Name] = null;
            return null;
        }

        var parentName = system.RunsOn.First();

        if (!systemsByName.TryGetValue(parentName, out var parent))
        {
            cache[system.Name] = null;
            return null;
        }

        var resolved = ResolveSystemIp(parent, systemsByName, cache);
        cache[system.Name] = resolved;

        return resolved;
    }
    private string? ResolveServiceIp(
        Service service,
        Dictionary<string, SystemResource> systemsByName,
        Dictionary<string, string?> cache)
    {
        // Direct IP wins
        if (!string.IsNullOrWhiteSpace(service.Network?.Ip))
            return service.Network!.Ip;

        // Must have exactly one parent
        if (service.RunsOn?.Count != 1)
            return null;

        var parentName = service.RunsOn.First();

        if (!systemsByName.TryGetValue(parentName, out var parent))
            return null;

        return ResolveSystemIp(parent, systemsByName, cache);
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
        var result = resourceCollection.Resources
            .Where(r => r.RunsOn.Any(p => p.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Task.FromResult<IReadOnlyList<Resource>>(result);
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

        var version = _deserializer.GetSchemaVersion(yaml); 
        
        // Guard: config is newer than this app understands.
        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Config schema version {version} is newer than this application supports ({CurrentSchemaVersion}).");
        }

        YamlRoot? root;
        // If older, backup first, then migrate step-by-step, then save.
        if (version < CurrentSchemaVersion)
        {
            await BackupOriginalAsync(yaml);

            root = await _deserializer.Deserialize(yaml) ?? new YamlRoot();
            
            // Ensure we persist the migrated root (with updated version)
            await SaveRootAsync(root);
        }
        else
        {
            root = await _deserializer.Deserialize(yaml);
        }
        
        resourceCollection.Resources.Clear();
        if (root?.Resources != null)
        {
            resourceCollection.Resources.AddRange(root.Resources);
        }
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
                Version = CurrentSchemaVersion,
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
    
    private async Task SaveRootAsync(YamlRoot? root)
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
            ["version"] = root.Version,
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
    public int Version { get; set; }
    public List<Resource>? Resources { get; set; }
}
