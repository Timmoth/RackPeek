namespace RackPeek.Domain.Resources.Models;

public class Device : Hardware
{
    public const string KindLabel = "Device";
    public string? Model { get; set; }
}
