using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.OtherHardware;
using RackPeek.Domain.Resources.UpsUnits;

namespace RackPeek.Domain.Resources;

public abstract class Resource {
    private static readonly string[] _hardwareTypes =
        ["server", "switch", "firewall", "router", "accesspoint", "desktop", "laptop", "ups", "other"];

    private static readonly Dictionary<string, string> _kindToPluralDictionary = new()
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
        { "other", "other" },
        { "system", "systems" },
        { "service", "services" }
    };

    private static readonly Dictionary<Type, string> _typeToKindMap = new()
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
        { typeof(Other), "Other" },
        { typeof(SystemResource), "System" },
        { typeof(Service), "Service" }
    };

    public string Kind { get; set; } = string.Empty;

    public required string Name { get; set; }

    /// <summary>
    ///     Stable machine-generated identity, set by <c>rpk discover</c>. Optional, and
    ///     absent on everything entered by hand. Lets a re-run find this resource again
    ///     after the user has renamed it. See <c>RackPeek.Domain.Discovery.DiscoveryId</c>.
    /// </summary>
    public string? DiscoveryId { get; set; }

    /// <summary>
    ///     Whether a person chose this name. Set the moment anyone renames the resource,
    ///     and never unset.
    ///     <para>
    ///         A discovered resource starts out named by whatever the collector could see,
    ///         which is often a placeholder derived from its own id. Later runs — or a
    ///         better collector — may learn the machine's real name, and should be able to
    ///         improve on a placeholder. They must never touch a name a person typed.
    ///     </para>
    ///     <para>
    ///         Absent means "not stated". A resource with no <see cref="DiscoveryId" /> was
    ///         entered by hand and is therefore user-named whatever this says; see
    ///         <see cref="IsUserNamed" />.
    ///     </para>
    /// </summary>
    public bool? UserNamed { get; set; }

    /// <summary>
    ///     Whether this resource's name is a person's choice and so off limits to
    ///     discovery. True when the flag says so, and true for anything with no
    ///     discovery id at all — nothing but a person could have written it.
    /// </summary>
    /// <remarks>
    ///     A method rather than a property because everything public on a resource is
    ///     serialised, and this is derived from what is stored rather than part of it.
    /// </remarks>
    public bool IsUserNamed() => UserNamed ?? string.IsNullOrWhiteSpace(DiscoveryId);

    public string[] Tags { get; set; } = [];
    public Dictionary<string, string> Labels { get; set; } = new();
    public string? Notes { get; set; }

    public List<string> RunsOn { get; set; } = new();

    public static bool IsHardware(string kind) {
        kind = kind.Trim().ToLower();
        return kind == "hardware" || _hardwareTypes.Contains(kind);
    }

    public static string GetResourceUrl(string kind, string name) {
        var encoded = Uri.EscapeDataString(name);

        kind = kind.Trim().ToLower();
        if (IsHardware(kind)) return $"resources/hardware/{encoded}";

        if (kind == "system") return $"resources/systems/{encoded}";

        if (kind == "service") return $"resources/services/{encoded}";

        return "#";
    }

    public static string KindToPlural(string kind) =>
        _kindToPluralDictionary.GetValueOrDefault(kind.ToLower().Trim(), kind);

    public static string GetKind<T>() where T : Resource {
        if (_typeToKindMap.TryGetValue(typeof(T), out var kind))
            return kind;

        throw new InvalidOperationException(
            $"No kind mapping defined for type {typeof(T).Name}");
    }

    public static bool CanRunOn<T>(Resource parent) where T : Resource {
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
