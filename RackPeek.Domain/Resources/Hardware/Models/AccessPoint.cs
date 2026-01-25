using RackPeek.Domain.Resources.Hardware.Parsing;

namespace RackPeek.Domain.Resources.Hardware.Models;

public class AccessPoint : Hardware
{
    public string? Model { get; set; }
    public string? Speed { get; set; }
    public double SpeedGb => UnitParser.ParseGbValue(Speed);
}