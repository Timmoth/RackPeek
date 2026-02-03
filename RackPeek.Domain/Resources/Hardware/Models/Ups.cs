namespace RackPeek.Domain.Resources.Hardware.Models;

public class Ups : Hardware
{
    public string? Model { get; set; }
    public int? Va { get; set; }
    
    public const string KindLabel = "Ups";

}