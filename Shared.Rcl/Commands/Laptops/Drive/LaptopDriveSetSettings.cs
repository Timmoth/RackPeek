using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Drive;

public class LaptopDriveSetSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The drive index to update.")]
    public int Index { get; set; }

    [CommandOption("--type")]
    [Description("The drive type e.g hdd / ssd.")]
    public string? Type { get; set; }

    [CommandOption("--size")]
    [Description("The drive capacity in Gb.")]
    public int? Size { get; set; }
}