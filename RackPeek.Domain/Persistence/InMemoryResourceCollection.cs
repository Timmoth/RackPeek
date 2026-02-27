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
