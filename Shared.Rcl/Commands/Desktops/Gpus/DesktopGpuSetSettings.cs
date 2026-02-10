using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Gpus;

public class DesktopGpuSetSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the gpu to update.")]
    public int Index { get; set; }

    [CommandOption("--model")]
    [Description("The gpu model name.")]
    public string? Model { get; set; }

    [CommandOption("--vram")]
    [Description("The amount of gpu vram in Gb.")]
    public int? Vram { get; set; }
}