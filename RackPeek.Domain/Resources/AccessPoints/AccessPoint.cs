namespace RackPeek.Domain.Resources.AccessPoints;

public class AccessPoint : Hardware.Hardware {
    public const string KindLabel = "AccessPoint";
    public double? Speed { get; set; }
}
