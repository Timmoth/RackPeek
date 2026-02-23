using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Servers;

public interface ICpuResource
{
    public List<Cpu>? Cpus { get; set; }
}