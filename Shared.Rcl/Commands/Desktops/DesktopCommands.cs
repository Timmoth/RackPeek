using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops;

public class DesktopNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}