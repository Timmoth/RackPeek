using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops;

public class LaptopNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}