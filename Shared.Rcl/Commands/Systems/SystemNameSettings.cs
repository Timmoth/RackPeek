using System.ComponentModel;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Systems;

public class SystemNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The name of the system.")]
    public string Name { get; set; } = default!;
}