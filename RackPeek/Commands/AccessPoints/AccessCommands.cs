using Spectre.Console.Cli;

namespace RackPeek.Commands.AccessPoints;

public class AccessPointNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] 
    public string Name { get; set; } = default!;
}