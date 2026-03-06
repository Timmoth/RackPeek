namespace RackPeek.Domain.Resources.OtherHardware;

public class Other : Hardware.Hardware
{
    public const string KindLabel = "Other";
    public string? Model { get; set; }
    public string? Description { get; set; }
}
