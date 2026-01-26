using Spectre.Console.Cli;

namespace RackPeek.Commands.Systems;

public class SystemNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}