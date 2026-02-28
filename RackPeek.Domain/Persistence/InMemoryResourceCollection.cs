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

namespace RackPeek.Domain.Persistence;

public sealed class InMemoryResourceCollection(IEnumerable<Resource>? seed = null) : IResourceCollection
{
    private readonly object _lock = new();
    private readonly List<Resource> _resources = seed?.ToList() ?? [];

    public IReadOnlyList<Hardware> HardwareResources
    {
        get
        {
            lock (_lock)
            {
                return _resources.OfType<Hardware>().ToList();
            }
        }
    }

    public IReadOnlyList<SystemResource> SystemResources
    {
        get
        {
            lock (_lock)
            {
                return _resources.OfType<SystemResource>().ToList();
            }
        }
    }

    public IReadOnlyList<Service> ServiceResources
    {
        get
        {
            lock (_lock)
            {
                return _resources.OfType<Service>().ToList();
            }
        }
    }

    public Task<bool> Exists(string name)
    {
        lock (_lock)
        {
            return Task.FromResult(_resources.Exists(r =>
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<string?> GetKind(string name)
    {
        lock (_lock)
        {
            return Task.FromResult(_resources.FirstOrDefault(r =>
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Kind);
        }
        
    }

    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Resource>> GetByTagAsync(string name)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Resource>>(_resources.Where(r => r.Tags.Contains(name)).ToList());
        }
    }

    public Task<Dictionary<string, int>> GetTagsAsync()
    {
        lock (_lock)
        {
            var result = _resources
                .SelectMany(r => r.Tags!) // flatten all tag arrays
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .GroupBy(t => t)
                .ToDictionary(g => g.Key, g => g.Count());
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<(Resource, string)>> GetByLabelAsync(string name)
    {
        lock (_lock)
        {
            var result = _resources
                .Select(r =>
                {
                    if (r.Labels != null && r.Labels.TryGetValue(name, out var value))
                        return (found: true, pair: (r, value));

                    return (found: false, pair: default);
                })
                .Where(x => x.found)
                .Select(x => x.pair)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<(Resource, string)>>(result);
        }
    }

    public Task<Dictionary<string, int>> GetLabelsAsync()
    {
        lock (_lock)
        {
            var result = _resources
                .SelectMany(r => r.Labels ?? Enumerable.Empty<KeyValuePair<string, string>>())
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            return Task.FromResult(result);
        }
    }

      public Task<IReadOnlyList<(Resource, string)>> GetResourceIpsAsync()
    {        lock (_lock)
        {
        var result = new List<(Resource, string)>();

        var allResources = _resources;

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
    
    public Task<IReadOnlyList<T>> GetAllOfTypeAsync<T>()
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<T>>(_resources.OfType<T>().ToList());
        }
    }

    public Task<IReadOnlyList<Resource>> GetDependantsAsync(string name)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Resource>>(_resources
                .Where(r => r.RunsOn.Select(p => p.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList().Count != 0).ToList());
        }
    }


    public Task AddAsync(Resource resource)
    {
        lock (_lock)
        {
            if (_resources.Any(r =>
                    r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"'{resource.Name}' already exists.");

            resource.Kind = GetKind(resource);
            _resources.Add(resource);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Resource resource)
    {
        lock (_lock)
        {
            var index = _resources.FindIndex(r =>
                r.Name.Equals(resource.Name, StringComparison.OrdinalIgnoreCase));

            if (index == -1)
                throw new InvalidOperationException("Not found.");

            resource.Kind = GetKind(resource);
            _resources[index] = resource;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name)
    {
        lock (_lock)
        {
            _resources.RemoveAll(r =>
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        return Task.CompletedTask;
    }

    public Task<Resource?> GetByNameAsync(string name)
    {
        lock (_lock)
        {
            return Task.FromResult(_resources.FirstOrDefault(r =>
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<T?> GetByNameAsync<T>(string name) where T : Resource
    {
        lock (_lock)
        {
            var resource = _resources.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<T?>(resource as T);
        }
    }

    public Resource? GetByName(string name)
    {
        lock (_lock)
        {
            return _resources.FirstOrDefault(r =>
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string GetKind(Resource resource)
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
            _ => throw new InvalidOperationException(
                $"Unknown resource type: {resource.GetType().Name}")
        };
    }
}
