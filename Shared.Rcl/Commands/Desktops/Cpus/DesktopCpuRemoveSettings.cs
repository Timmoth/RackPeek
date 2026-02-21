using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Cpus;

public class DesktopCpuRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The name of the desktop.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the desktop cpu to remove.")]
    public int Index { get; set; }
}