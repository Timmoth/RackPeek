using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Laptops;

public class Laptop : Hardware.Hardware, ICpuResource, IDriveResource, IGpuResource
{
    public const string KindLabel = "Laptop";
    public Ram? Ram { get; set; }
    public string? Model { get; set; }
    public List<Cpu>? Cpus { get; set; }
    public List<Drive>? Drives { get; set; }
    public List<Gpu>? Gpus { get; set; }
}