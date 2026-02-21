using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Gpus;

public class LaptopGpuRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the Gpu to remove.")]
    public int Index { get; set; }
}