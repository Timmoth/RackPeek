using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Nics;

public class LaptopNicSetSettings : CommandSettings {
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the nic to update.")]
    public int Index { get; set; }

    [CommandOption("--type")]
    [Description("The nic port type e.g rj45 / sfp+")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("The port speed.")]
    public double? Speed { get; set; }

    [CommandOption("--ports")]
    [Description("The number of ports.")]
    public int? Ports { get; set; }
}
