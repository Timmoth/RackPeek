using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.UpsUnits;

namespace RackPeek.Domain.Resources;

public abstract class Resource
{
    private static readonly Dictionary<string, string> KindToPluralDictionary = new()
    {
        { "hardware", "hardware" },
        { "server", "servers" },
        { "switch", "switches" },
        { "firewall", "firewalls" },
        { "router", "routers" },
        { "accesspoint", "accesspoints" },
        { "desktop", "desktops" },
        { "laptop", "laptops" },
        { "ups", "ups" },
        { "system", "systems" },
        { "service", "services" }
    };

    private static readonly Dictionary<Type, string> TypeToKindMap = new()
    {
        { typeof(Hardware.Hardware), "Hardware" },
        { typeof(Server), "Server" },
        { typeof(Switch), "Switch" },
        { typeof(Firewall), "Firewall" },
        { typeof(Router), "Router" },
        { typeof(AccessPoint), "Accesspoint" },
        { typeof(Desktop), "Desktop" },
        { typeof(Laptop), "Laptop" },
        { typeof(Ups), "Ups" },
        { typeof(SystemResource), "System" },
        { typeof(Service), "Service" }
    };

    public string Kind { get; set; } = string.Empty;

    public required string Name { get; set; }

    public string[] Tags { get; set; } = [];
    public Dictionary<string, string> Labels { get; set; } = new();
    public string? Notes { get; set; }

    public List<string> RunsOn { get; set; } = new List<string>();

    public static string KindToPlural(string kind)
    {
        return KindToPluralDictionary.GetValueOrDefault(kind.ToLower().Trim(), kind);
    }

    public static string GetKind<T>() where T : Resource
    {
        if (TypeToKindMap.TryGetValue(typeof(T), out var kind))
            return kind;

        throw new InvalidOperationException(
            $"No kind mapping defined for type {typeof(T).Name}");
    }

    public static bool CanRunOn<T>(Resource parent) where T : Resource
    {
        var childKind = GetKind<T>().ToLowerInvariant();
        var parentKind = parent.Kind.ToLowerInvariant();

        // Service -> System
        if (childKind == "service" && parentKind == "system")
            return true;

        // System -> Hardware
        if (childKind == "system" && parent is Hardware.Hardware)
            return true;
        
        // System -> System
        if (childKind == "system" && parent is SystemResource)
            return true;

        return false;
    }
}
