using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Desktops;

public class Desktop : Hardware.Hardware, ICpuResource, IDriveResource, IGpuResource, INicResource
{
    public const string KindLabel = "Desktop";
    public Ram? Ram { get; set; }
    public string Model { get; set; }
    public List<Cpu>? Cpus { get; set; }
    public List<Drive>? Drives { get; set; }
    public List<Gpu>? Gpus { get; set; }
    public List<Nic>? Nics { get; set; }
}