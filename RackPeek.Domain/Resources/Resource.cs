using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

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

    public string Kind { get; set; } = string.Empty;

    public required string Name { get; set; }

    public string[]? Tags { get; set; } = [];
    public string? Notes { get; set; }
    
    public string? RunsOn { get; set; }

    public static string KindToPlural(string kind)
    {
        return KindToPluralDictionary.GetValueOrDefault(kind.ToLower().Trim(), kind);
    }
    
    private static readonly Dictionary<Type, string> TypeToKindMap = new()
    {
        { typeof(Models.Hardware), "Hardware" },
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
        if (childKind == "system" && parent is Models.Hardware)
            return true;

        return false;
    }

}