using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Nics;

public class DesktopNicSetSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the nic to remove.")]
    public int Index { get; set; }

    [CommandOption("--type")]
    [Description("The nic port type e.g rj45 / sfp+")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("The speed of the nic in Gb/s.")]
    public int? Speed { get; set; }

    [CommandOption("--ports")]
    [Description("The number of ports.")]
    public int? Ports { get; set; }
}