using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Gpus;

public class DesktopGpuAddSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The name of the desktop.")]
    public string DesktopName { get; set; } = default!;

    [CommandOption("--model")]
    [Description("The Gpu model.")]
    public string? Model { get; set; }

    [CommandOption("--vram")]
    [Description("The amount of gpu vram in Gb.")]
    public int? Vram { get; set; }
}