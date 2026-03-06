using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Servers;

public class Server : Hardware.Hardware, ICpuResource, IDriveResource, IGpuResource, IPortResource {
    public const string KindLabel = "Server";
    public Ram? Ram { get; set; }
    public bool? Ipmi { get; set; }
    public List<Cpu>? Cpus { get; set; }
    public List<Drive>? Drives { get; set; }
    public List<Gpu>? Gpus { get; set; }
    public List<Port>? Ports { get; set; }
}
