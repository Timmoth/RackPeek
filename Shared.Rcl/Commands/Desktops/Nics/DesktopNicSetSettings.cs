using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Nics;

public class DesktopNicSetSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the nic to remove.")]
    public int Index { get; set; }

    [CommandOption("--type")]
    [Description("NIC type: rj45, sfp, sfp+, sfp28, sfp56, qsfp+, qsfp28, qsfp56, qsfp-dd, osfp, xfp, cx4, mgmt.")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("The speed of the nic in Gb/s.")]
    public double? Speed { get; set; }

    [CommandOption("--ports")]
    [Description("The number of ports.")]
    public int? Ports { get; set; }
}