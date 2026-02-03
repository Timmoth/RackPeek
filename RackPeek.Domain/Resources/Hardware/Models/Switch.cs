namespace RackPeek.Domain.Resources.Hardware.Models;

public class Switch : Hardware
{
    public string? Model { get; set; }
    public bool? Managed { get; set; }
    public bool? Poe { get; set; }
    public List<Port>? Ports { get; set; }
    
    public const string KindLabel = "Switch";

}