using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Switches;

public class Switch : Hardware.Hardware, IPortResource
{
    public const string KindLabel = "Switch";
    public string? Model { get; set; }
    public bool? Managed { get; set; }
    public bool? Poe { get; set; }
    public List<Port>? Ports { get; set; }
}