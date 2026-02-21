namespace RackPeek.Domain.Resources.UpsUnits;

public class Ups : Hardware.Hardware
{
    public const string KindLabel = "Ups";
    public string? Model { get; set; }
    public int? Va { get; set; }
}