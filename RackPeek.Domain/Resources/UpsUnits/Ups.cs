using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.UpsUnits;

public class Ups : Hardware.Hardware, IPortResource {
    public const string KindLabel = "Ups";
    public string? Model { get; set; }
    public int? Va { get; set; }
    public List<Port>? Ports { get; set; }
}
