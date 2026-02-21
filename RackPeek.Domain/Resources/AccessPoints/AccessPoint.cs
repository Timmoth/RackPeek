namespace RackPeek.Domain.Resources.AccessPoints;

public class AccessPoint : Hardware.Hardware
{
    public const string KindLabel = "AccessPoint";
    public string? Model { get; set; }
    public double? Speed { get; set; }
}