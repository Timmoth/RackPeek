using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Exporters;

public sealed class GenerateHostsFileSettings : CommandSettings {
    [CommandOption("--include-tags")]
    [Description("Comma-separated list of tags to include (e.g. prod,staging)")]
    public string? IncludeTags { get; init; }

    [CommandOption("--domain-suffix")]
    [Description("Optional domain suffix to append (e.g. home.local)")]
    public string? DomainSuffix { get; init; }

    [CommandOption("--no-localhost")]
    [Description("Do not include localhost defaults")]
    public bool NoLocalhost { get; init; }

    [CommandOption("-o|--output")]
    [Description("Write hosts file to file instead of stdout")]
    public string? OutputPath { get; init; }
}
