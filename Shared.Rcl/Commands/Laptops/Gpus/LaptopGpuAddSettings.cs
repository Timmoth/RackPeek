using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Gpus;

public class LaptopGpuAddSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The name of the Laptop.")]
    public string LaptopName { get; set; } = default!;

    [CommandOption("--model")]
    [Description("The Gpu model.")]
    public string? Model { get; set; }

    [CommandOption("--vram")]
    [Description("The amount of gpu vram in Gb.")]
    public int? Vram { get; set; }
}