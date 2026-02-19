using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Services;

public class ServiceNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The name of the service.")]
    public string Name { get; set; } = default!;
}