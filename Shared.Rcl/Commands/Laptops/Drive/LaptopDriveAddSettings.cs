using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Drive;

public class LaptopDriveAddSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The name of the Laptop.")]
    public string LaptopName { get; set; } = default!;

    [CommandOption("--type")]
    [Description("Drive type: nvme, ssd, hdd, sas, sata, usb, sdcard, micro-sd.")]
    public string? Type { get; set; }

    [CommandOption("--size")]
    [Description("The drive capacity in Gb.")]
    public int? Size { get; set; }
}