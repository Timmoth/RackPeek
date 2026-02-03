namespace RackPeek.Domain.Resources.Hardware.Models;

public class Laptop : Hardware
{
    public List<Cpu>? Cpus { get; set; }
    public Ram? Ram { get; set; }
    public List<Drive>? Drives { get; set; }
    public List<Gpu>? Gpus { get; set; }
    
    public const string KindLabel = "Laptop";

}