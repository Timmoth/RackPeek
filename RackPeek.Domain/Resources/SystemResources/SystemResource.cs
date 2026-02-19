using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.SystemResources;

public class SystemResource : Resource, IDriveResource
{
    public const string KindLabel = "System";

    public static readonly string[] ValidSystemTypes =
    [
        "baremetal",
        "hypervisor",
        "vm",
        "container",
        "embedded",
        "cloud",
        "other"
    ];

    public string? Type { get; set; }
    public string? Os { get; set; }
    public int? Cores { get; set; }
    public int? Ram { get; set; }
    public List<Drive>? Drives { get; set; }
}