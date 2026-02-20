namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public class AccessPoint : Hardware
{
    public const string KindLabel = "AccessPoint";
    public string? Model { get; set; }
    public double? Speed { get; set; }
}