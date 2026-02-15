using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Drive;

public class DesktopDriveAddSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The name of the desktop.")]
    public string DesktopName { get; set; } = default!;

    [CommandOption("--type")]
    [Description("Drive type: nvme, ssd, hdd, sas, sata, usb, sdcard, micro-sd.")]
    public string? Type { get; set; }

    [CommandOption("--size")]
    [Description("The drive capacity in Gb.")]
    public int? Size { get; set; }
}