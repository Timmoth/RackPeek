using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers;

public class ServerNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}