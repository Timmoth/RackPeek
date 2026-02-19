using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Drive;

public class LaptopDriveRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The name of the Laptop.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the drive to remove.")]
    public int Index { get; set; }
}