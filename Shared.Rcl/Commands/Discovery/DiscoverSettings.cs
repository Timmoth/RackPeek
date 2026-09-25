using System.ComponentModel;
using RackPeek.Domain.Discovery;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Discovery;

/// <summary>Output and upload options shared by every <c>rpk discover</c> command.</summary>
public abstract class DiscoverSettings : CommandSettings {
    [CommandOption("--push")]
    [Description("Upload the result to a RackPeek server instead of printing it.")]
    public bool Push { get; init; }

    [CommandOption("--server <URL>")]
    [Description("RackPeek server to upload to. Defaults to the RPK_SERVER environment variable.")]
    public string? Server { get; init; }

    [CommandOption("--api-key <KEY>")]
    [Description("API key for the server. Defaults to the RPK_API_KEY environment variable.")]
    public string? ApiKey { get; init; }

    [CommandOption("--dry-run")]
    [Description("Ask the server what would change, without changing anything. Implies --push.")]
    public bool DryRun { get; init; }

    public bool ShouldUpload => Push || DryRun;

    public string? ResolvedServer => DiscoveryPublisher.ResolveServer(Server);

    public string? ResolvedApiKey => DiscoveryPublisher.ResolveApiKey(ApiKey);

    public override ValidationResult Validate() {
        if (!ShouldUpload)
            return ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(ResolvedServer))
            return ValidationResult.Error(
                "No server to upload to. Pass --server or set RPK_SERVER.");

        if (string.IsNullOrWhiteSpace(ResolvedApiKey))
            return ValidationResult.Error(
                "No API key. Pass --api-key or set RPK_API_KEY.");

        return ValidationResult.Success();
    }
}
