using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Gpus;

public class DesktopGpuRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the Gpu to remove.")]
    public int Index { get; set; }
}