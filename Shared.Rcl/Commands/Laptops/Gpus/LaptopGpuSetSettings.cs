using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Gpus;

public class LaptopGpuSetSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

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