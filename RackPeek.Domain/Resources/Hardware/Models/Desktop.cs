namespace RackPeek.Domain.Resources.Hardware.Models;

public class Desktop : Hardware
{
    public List<Cpu>? Cpus { get; set; }
    public Ram? Ram { get; set; }
    public List<Drive>? Drives { get; set; }
    public List<Nic>? Nics { get; set; }
    public List<Gpu>? Gpus { get; set; }
    public string Model { get; set; }
    
    public const string KindLabel = "Desktop";

}