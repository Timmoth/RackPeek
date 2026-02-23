using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Servers;

public interface IPortResource
{
    public List<Port>? Ports { get; set; }
}