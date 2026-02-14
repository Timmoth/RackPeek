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

    public static string KindToPlural(string kind)
    {
        return KindToPluralDictionary.GetValueOrDefault(kind.ToLower().Trim(), kind);
    }
}