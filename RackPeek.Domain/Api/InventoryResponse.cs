namespace RackPeek.Domain.Api;

public class ImportYamlResponse {
    public List<string> Added { get; set; } = new();
    public List<string> Updated { get; set; } = new();
    public List<string> Replaced { get; set; } = new();

    public Dictionary<string, string> Renamed { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> OldYaml { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> NewYaml { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public List<string> ConnectionsAdded { get; set; } = new();
    public List<string> ConnectionsRemoved { get; set; } = new();
}
