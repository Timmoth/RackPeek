using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}