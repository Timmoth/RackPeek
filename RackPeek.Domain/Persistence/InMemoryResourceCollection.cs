using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

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
                return _resources.OfType<Hardware>().ToList();
        }
    }

    public IReadOnlyList<SystemResource> SystemResources
    {
        get
        {
            lock (_lock)
                return _resources.OfType<SystemResource>().ToList();
        }
    }

    public IReadOnlyList<Service> ServiceResources
    {
        get
        {
            lock (_lock)
                return _resources.OfType<Service>().ToList();
        }
    }

    public Task LoadAsync()
        => Task.CompletedTask;

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

    public Resource? GetByName(string name)
    {
        lock (_lock)
        {
            return _resources.FirstOrDefault(r =>
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string GetKind(Resource resource) => resource switch
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
