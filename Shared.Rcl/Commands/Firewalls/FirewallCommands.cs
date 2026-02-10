using Spectre.Console.Cli;

namespace RackPeek.Commands.Firewalls;

public class FirewallNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}