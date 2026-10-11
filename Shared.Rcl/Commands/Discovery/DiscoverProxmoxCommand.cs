using System.ComponentModel;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Discovery;

public sealed class DiscoverProxmoxSettings : DiscoverSettings {
    [CommandOption("--host <URL>")]
    [Description("Proxmox host, e.g. https://pve.lan:8006. A bare host name gets https and :8006.")]
    public string? Host { get; init; }

    [CommandOption("--token-id <ID>")]
    [Description("API token id, e.g. root@pam!rackpeek. Defaults to RPK_PVE_TOKEN_ID.")]
    public string? TokenId { get; init; }

    [CommandOption("--token-secret <SECRET>")]
    [Description("API token secret. Defaults to RPK_PVE_TOKEN_SECRET.")]
    public string? TokenSecret { get; init; }

    [CommandOption("--insecure")]
    [Description("Accept a self-signed certificate, which Proxmox ships with by default.")]
    public bool Insecure { get; init; }

    public string? ResolvedTokenId =>
        DiscoveryPublisher.Resolve(TokenId, ProxmoxApiClient.TokenIdEnvironmentVariable);

    public string? ResolvedTokenSecret =>
        DiscoveryPublisher.Resolve(TokenSecret, ProxmoxApiClient.TokenSecretEnvironmentVariable);

    public override ValidationResult Validate() {
        if (string.IsNullOrWhiteSpace(Host))
            return ValidationResult.Error("Pass --host, e.g. --host https://pve.lan:8006");

        if (string.IsNullOrWhiteSpace(ResolvedTokenId))
            return ValidationResult.Error(
                $"No API token id. Pass --token-id or set {ProxmoxApiClient.TokenIdEnvironmentVariable}.");

        if (string.IsNullOrWhiteSpace(ResolvedTokenSecret))
            return ValidationResult.Error(
                $"No API token secret. Pass --token-secret or set {ProxmoxApiClient.TokenSecretEnvironmentVariable}.");

        return base.Validate();
    }
}

/// <summary>
///     Reads a Proxmox estate and emits its nodes and guests as Systems, already wired
///     together — which is the part that is tedious to type by hand.
/// </summary>
public sealed class DiscoverProxmoxCommand : AsyncCommand<DiscoverProxmoxSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        DiscoverProxmoxSettings settings,
        CancellationToken cancellationToken) {
        ProxmoxApiClient client;

        try {
            client = new ProxmoxApiClient(
                settings.Host!,
                settings.ResolvedTokenId!,
                settings.ResolvedTokenSecret!,
                settings.Insecure);
        }
        catch (UriFormatException ex) {
            AnsiConsole.MarkupLine(
                $"[red]'{Markup.Escape(settings.Host!)}' is not a usable host.[/] {Markup.Escape(ex.Message)}");

            return 1;
        }

        List<Resource> resources;

        try {
            resources = await ProxmoxDiscovery.ReadAsync(client, cancellationToken);
        }
        catch (HttpRequestException ex) {
            AnsiConsole.MarkupLine(
                $"[red]Could not read {Markup.Escape(client.Endpoint)}.[/] {Markup.Escape(ex.Message)}");

            if (!settings.Insecure && ex.InnerException is System.Security.Authentication.AuthenticationException)
                AnsiConsole.MarkupLine(
                    "[yellow]Proxmox uses a self-signed certificate by default — try --insecure.[/]");

            return 1;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) {
            // HttpClient reports its timeout as a cancellation.
            AnsiConsole.MarkupLine(
                $"[red]{Markup.Escape(client.Endpoint)} did not answer within the timeout.[/]");

            return 1;
        }
        finally {
            client.Dispose();
        }

        return await DiscoveryOutput.EmitAsync(resources, settings, cancellationToken);
    }
}
