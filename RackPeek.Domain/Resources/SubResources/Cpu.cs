namespace RackPeek.Domain.Resources.SubResources;

public class Cpu
{
    public string? Model { get; set; }
    public int? Cores { get; set; }
    public int? Threads { get; set; }

    public override string ToString()
    {
        return $"{Model} {Cores} {Threads}";
    }
}