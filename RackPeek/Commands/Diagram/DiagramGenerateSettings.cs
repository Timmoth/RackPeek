using Spectre.Console.Cli;

namespace RackPeek.Commands.Diagram;

public class DiagramGenerateSettings : CommandSettings
{
    [CommandOption("-o|--output")]
    public string Output { get; set; } = "rackpeek.drawio";
}