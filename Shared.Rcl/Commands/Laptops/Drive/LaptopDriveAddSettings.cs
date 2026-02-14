using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Drive;

public class LaptopDriveAddSettings : CommandSettings
{
    [CommandArgument(0, "<laptop>")]
    [Description("The name of the Laptop.")]
    public string LaptopName { get; set; } = default!;

    [CommandOption("--type")]
    [Description("The drive type e.g hdd / ssd.")]
    public string? Type { get; set; }

    [CommandOption("--size")]
    [Description("The drive capacity in GB:.")]
    public int? Size { get; set; }
}