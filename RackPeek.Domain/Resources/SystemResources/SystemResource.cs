using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.SystemResources;

public class SystemResource : Resource
{
    public string? Type { get; set; }
    public string? Os { get; set; }
    public int? Cores { get; set; }
    public int? Ram { get; set; }
    public List<Drive>? Drives { get; set; }

    public string? RunsOn { get; set; }

    public const string KindLabel = "System";
}