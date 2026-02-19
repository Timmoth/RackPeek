using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Cpus;

public class DesktopCpuAddSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandOption("--model")]
    [Description("The model name.")]
    public string? Model { get; set; }

    [CommandOption("--cores")]
    [Description("The number of cpu cores.")]
    public int? Cores { get; set; }

    [CommandOption("--threads")]
    [Description("The number of cpu threads.")]
    public int? Threads { get; set; }
}