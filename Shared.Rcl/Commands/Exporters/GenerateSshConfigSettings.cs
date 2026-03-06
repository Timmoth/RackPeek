using System.ComponentModel;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Exporters;

public sealed class GenerateSshConfigSettings : CommandSettings {
    [CommandOption("--include-tags")]
    [Description("Comma-separated list of tags to include (e.g. prod,linux)")]
    public string? IncludeTags { get; init; }

    [CommandOption("--default-user")]
    [Description("Default SSH user if not defined in labels")]
    public string? DefaultUser { get; init; }

    [CommandOption("--default-port")]
    [Description("Default SSH port if not defined in labels (default: 22)")]
    [DefaultValue(22)]
    public int DefaultPort { get; init; } = 22;

    [CommandOption("--default-identity")]
    [Description("Default SSH identity file (e.g. ~/.ssh/id_rsa)")]
    public string? DefaultIdentityFile { get; init; }

    [CommandOption("-o|--output")]
    [Description("Write SSH config to file instead of stdout")]
    public string? OutputPath { get; init; }
}
