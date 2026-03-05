namespace RackPeek.Domain.Resources.UpsUnits;

public class Ups : Hardware.Hardware {
    public const string KindLabel = "Ups";
    public int? Va { get; set; }
}
