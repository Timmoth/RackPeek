using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops;

public class DesktopNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}