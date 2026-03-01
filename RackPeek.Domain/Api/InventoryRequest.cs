using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;

namespace RackPeek.Domain.Api;

public class ImportYamlRequest
{
    public string? Yaml { get; set; }
    public object? Json { get; set; } 
    public MergeMode Mode { get; set; } = MergeMode.Merge;

    public bool DryRun { get; set; } = false;
}