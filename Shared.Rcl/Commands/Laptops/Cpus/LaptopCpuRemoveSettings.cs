using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Cpus;

public class LaptopCpuRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The name of the Laptop.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the Laptop cpu to remove.")]
    public int Index { get; set; }
}