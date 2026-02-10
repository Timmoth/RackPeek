using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Drive;

public class DesktopDriveRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The name of the desktop.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the drive to remove.")]
    public int Index { get; set; }
}