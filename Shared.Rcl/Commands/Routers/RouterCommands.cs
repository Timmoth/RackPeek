using Spectre.Console.Cli;

namespace RackPeek.Commands.Routers;

public class RouterNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}