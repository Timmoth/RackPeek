using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Servers;

public class Server : Hardware.Hardware, ICpuResource, IDriveResource, IGpuResource, INicResource
{
    public const string KindLabel = "Server";
    public Ram? Ram { get; set; }
    public bool? Ipmi { get; set; }
    public List<Cpu>? Cpus { get; set; }
    public List<Drive>? Drives { get; set; }
    public List<Gpu>? Gpus { get; set; }
    public List<Nic>? Nics { get; set; }
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

public interface IGpuResource
{
    public List<Gpu>? Gpus { get; set; }
}

public interface INicResource
{
    public List<Nic>? Nics { get; set; }
}