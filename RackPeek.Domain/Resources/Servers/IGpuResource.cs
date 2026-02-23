using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Servers;

public interface IGpuResource
{
    public List<Gpu>? Gpus { get; set; }
}