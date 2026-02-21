using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Cpus;

public class LaptopCpuSetSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the Laptop cpu.")]
    public int Index { get; set; }

    [CommandOption("--model")]
    [Description("The cpu model.")]
    public string? Model { get; set; }

    [CommandOption("--cores")]
    [Description("The number of cpu cores.")]
    public int? Cores { get; set; }

    [CommandOption("--threads")]
    [Description("The number of cpu threads.")]
    public int? Threads { get; set; }
}