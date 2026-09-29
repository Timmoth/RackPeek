using System.ComponentModel;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Discovery;

public sealed class DiscoverOpnsenseSettings : DiscoverSettings {
    [CommandOption("--host <URL>")]
    [Description("OPNsense host, e.g. https://firewall.lan. A bare host name gets https.")]
    public string? Host { get; init; }

    [CommandOption("--key <KEY>")]
    [Description("API key. Defaults to RPK_OPN_KEY.")]
    public string? Key { get; init; }

    [CommandOption("--secret <SECRET>")]
    [Description("API secret. Defaults to RPK_OPN_SECRET.")]
    public string? Secret { get; init; }

    [CommandOption("--insecure")]
    [Description("Accept a self-signed certificate, which OPNsense ships with by default.")]
    public bool Insecure { get; init; }

    [CommandOption("--include-public")]
    [Description("Also record neighbours on public addresses, such as the ISP equipment on the WAN leg.")]
    public bool IncludePublic { get; init; }

    public string? ResolvedKey =>
        DiscoveryPublisher.Resolve(Key, OpnsenseApiClient.KeyEnvironmentVariable);

    public string? ResolvedSecret =>
        DiscoveryPublisher.Resolve(Secret, OpnsenseApiClient.SecretEnvironmentVariable);

    public override ValidationResult Validate() {
        if (string.IsNullOrWhiteSpace(Host))
            return ValidationResult.Error("Pass --host, e.g. --host https://firewall.lan");

        if (string.IsNullOrWhiteSpace(ResolvedKey))
            return ValidationResult.Error(
                $"No API key. Pass --key or set {OpnsenseApiClient.KeyEnvironmentVariable}.");

        if (string.IsNullOrWhiteSpace(ResolvedSecret))
            return ValidationResult.Error(
                $"No API secret. Pass --secret or set {OpnsenseApiClient.SecretEnvironmentVariable}.");

        return base.Validate();
    }
}

/// <summary>
///     Reads an OPNsense firewall's neighbour table and emits every machine on it.
///     <para>
///         The collector for everything a sweep can see but not identify. ARP is
///         link-local, so sweeping from one host yields a MAC only for that host's own
///         segment and an address for everything else — and an address alone cannot
///         survive a DHCP re-lease or be matched to anything. The firewall routes every
///         subnet, so its table has the MAC for all of them.
///     </para>
/// </summary>
public sealed class DiscoverOpnsenseCommand : AsyncCommand<DiscoverOpnsenseSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        DiscoverOpnsenseSettings settings,
        CancellationToken cancellationToken) {
        using var client = new OpnsenseApiClient(
            settings.Host!,
            settings.ResolvedKey!,
            settings.ResolvedSecret!,
            settings.Insecure);

        List<Resource> resources;

        try {
            resources = await OpnsenseDiscovery.ReadAsync(client, settings.IncludePublic, cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or IOException or TimeoutException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)) {
            AnsiConsole.MarkupLine(
                $"[red]Could not read {Markup.Escape(client.Endpoint)}.[/] {Markup.Escape(ex.Message)}");

            // A self-signed certificate is the other common cause, and its message is
            // opaque enough to be worth naming.
            if (!settings.Insecure && ex is HttpRequestException { StatusCode: null })
                AnsiConsole.MarkupLine(
                    "[grey]If the firewall uses its own certificate, add --insecure.[/]");

            return 1;
        }

        if (resources.Count == 0)
            AnsiConsole.MarkupLine(
                "[grey]The firewall's ARP table is empty. It only holds neighbours it has "
                + "spoken to recently, so this is normal on a quiet network.[/]");

        return await DiscoveryOutput.EmitAsync(resources, settings, cancellationToken);
    }
}
