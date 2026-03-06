using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.AccessPoints;

public class AccessPoint : Hardware.Hardware, IPortResource {
    public const string KindLabel = "AccessPoint";
    public string? Model { get; set; }
    public double? Speed { get; set; }
    public List<Port>? Ports { get; set; }
}
