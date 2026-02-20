using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class Server : Hardware, ICpuResource, IDriveResource
{
    public const string KindLabel = "Server";
    public List<Cpu>? Cpus { get; set; }
    public Ram? Ram { get; set; }
    public List<Drive>? Drives { get; set; }
    public List<Nic>? Nics { get; set; }
    public List<Gpu>? Gpus { get; set; }
    public bool? Ipmi { get; set; }
}

public interface ICpuResource
{
    public List<Cpu>? Cpus { get; set; }
}

public interface IDriveResource
{
    public List<Drive>? Drives { get; set; }
}

public interface IPortResource
{
    public List<Port>? Ports { get; set; }
}