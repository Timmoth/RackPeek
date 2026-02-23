using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Servers;

public interface INicResource
{
    public List<Nic>? Nics { get; set; }
}