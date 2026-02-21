using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Routers;

public class Router : Hardware.Hardware, IPortResource
{
    public const string KindLabel = "Router";
    public string? Model { get; set; }
    public bool? Managed { get; set; }
    public bool? Poe { get; set; }
    public List<Port>? Ports { get; set; }
}