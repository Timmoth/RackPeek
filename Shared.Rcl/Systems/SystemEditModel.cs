using RackPeek.Domain.Resources.SystemResources;

namespace Shared.Rcl.Systems;

public sealed class SystemEditModel
{
    public string Name { get; init; } = default!;
    private string? _type;
    public string? Type
    {
        get => _type;
        set => _type = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }
    
    public string? Ip { get; set; }
    public string? Os { get; set; }
    public int? Cores { get; set; }
    public double? Ram { get; set; }
    public List<string> RunsOn { get; set; } = new List<string>();
    public string? Notes { get; set; }

    public static SystemEditModel From(SystemResource system)
    {
        return new SystemEditModel
        {
            Name = system.Name,
            Type = system.Type,
            Os = system.Os,
            Cores = system.Cores,
            Ram = system.Ram,
            Ip = system.Ip,
            RunsOn = system.RunsOn,
            Notes = system.Notes
        };
    }
}
