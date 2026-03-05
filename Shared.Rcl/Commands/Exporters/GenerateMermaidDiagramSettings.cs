using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Exporters;

public sealed class GenerateMermaidDiagramSettings : CommandSettings {
    [CommandOption("--include-tags")]
    [Description("Comma-separated list of tags to include (e.g. prod,linux)")]
    public string? IncludeTags { get; init; }

    [CommandOption("--diagram-type")]
    [Description("Mermaid diagram type (default: \"flowchart TD\")")]
    public string? DiagramType { get; init; }

    [CommandOption("--no-labels")]
    [Description("Disable resource label annotations")]
    public bool NoLabels { get; init; }

    [CommandOption("--no-edges")]
    [Description("Disable relationship edges")]
    public bool NoEdges { get; init; }

    [CommandOption("--label-whitelist")]
    [Description("Comma-separated list of label keys to include")]
    public string? LabelWhitelist { get; init; }

    [CommandOption("-o|--output")]
    [Description("Write Mermaid diagram to file instead of stdout")]
    public string? OutputPath { get; init; }
}
