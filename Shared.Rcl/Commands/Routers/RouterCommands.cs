using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers;

public class RouterNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}