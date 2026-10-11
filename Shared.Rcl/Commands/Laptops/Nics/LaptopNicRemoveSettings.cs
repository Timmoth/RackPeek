using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Nics;

public class LaptopNicRemoveSettings : CommandSettings {
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the nic to remove.")]
    public int Index { get; set; }
}
