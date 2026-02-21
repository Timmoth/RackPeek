using RackPeek.Domain.Resources.SystemResources;

namespace Shared.Rcl.Systems;

public sealed class SystemEditModel
{
    public string Name { get; init; } = default!;
    public string? Type { get; set; }
    public string? Os { get; set; }
    public int? Cores { get; set; }
    public double? Ram { get; set; }
    public string? RunsOn { get; set; }
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
            RunsOn = system.RunsOn,
            Notes = system.Notes
        };
    }
}