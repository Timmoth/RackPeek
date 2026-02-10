using Spectre.Console.Cli;

namespace RackPeek.Commands.Switches;

public class SwitchNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}