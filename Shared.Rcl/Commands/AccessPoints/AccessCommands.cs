using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.AccessPoints;

public class AccessPointNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The access point name.")]
    public string Name { get; set; } = default!;
}