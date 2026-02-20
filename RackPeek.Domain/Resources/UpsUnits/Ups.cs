namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public class Ups : Hardware
{
    public const string KindLabel = "Ups";
    public string? Model { get; set; }
    public int? Va { get; set; }
}