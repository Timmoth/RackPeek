namespace RackPeek.Domain.Resources.Hardware.Models;

public class AccessPoint : Hardware
{
    public string? Model { get; set; }
    public double? Speed { get; set; }
    
    public const string KindLabel = "AccessPoint";

}