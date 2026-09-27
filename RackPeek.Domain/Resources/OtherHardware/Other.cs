using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.OtherHardware;

public class Other : Hardware.Hardware, IPortResource {
    public const string KindLabel = "Other";
    public string? Model { get; set; }
    public string? Description { get; set; }
    public List<Port>? Ports { get; set; }
}
