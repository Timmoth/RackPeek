namespace RackPeek.Domain.Resources.Models;

public class Laptop : Hardware, ICpuResource, IDriveResource
{
    public const string KindLabel = "Laptop";
    public List<Cpu>? Cpus { get; set; }
    public Ram? Ram { get; set; }
    public List<Drive>? Drives { get; set; }
    public List<Gpu>? Gpus { get; set; }
    public string? Model { get; set; }
}