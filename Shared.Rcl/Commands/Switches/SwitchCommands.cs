using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches;

public class SwitchNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}