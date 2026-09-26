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
            resources = await ReadAsync(client, cancellationToken);
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

    private static async Task<List<Resource>> ReadAsync(
        IProxmoxClient client,
        CancellationToken cancellationToken) {
        var scope = await client.GetIdentityScopeAsync(cancellationToken);
        IReadOnlyList<ProxmoxNode> listed = await client.GetNodesAsync(cancellationToken);

        var nodes = new List<ProxmoxNode>();
        var guests = new List<ProxmoxGuest>();

        foreach (ProxmoxNode listedNode in listed) {
            // Node detail needs a broader permission than listing guests does, so it is
            // enrichment rather than a requirement — a read-only token still gets a tree.
            ProxmoxNode node = await client.EnrichAsync(listedNode, cancellationToken);
            nodes.Add(node);

            var nodeName = node.Name;

            foreach (var endpoint in new[] { ProxmoxApiClient.QemuEndpoint, ProxmoxApiClient.LxcEndpoint }) {
                IReadOnlyList<ProxmoxGuest> listedGuests =
                    await client.GetGuestsAsync(nodeName, endpoint, cancellationToken);

                // The list call knows nothing about the OS, and for a container it does
                // not know the address either. Both live in the guest's own config — one
                // call per guest, so they run concurrently rather than one at a time.
                ProxmoxGuestConfig[] configs = await Task.WhenAll(listedGuests.Select(g =>
                    client.GetGuestConfigAsync(nodeName, endpoint, g.VmId, cancellationToken)));

                for (var i = 0; i < listedGuests.Count; i++)
                    guests.Add(listedGuests[i] with {
                        Os = configs[i].Os,
                        Ip = configs[i].Ip,
                        Disks = configs[i].DiskBytes,
                        PassthroughAddresses = configs[i].PassthroughAddresses
                    });
            }
        }

        return ProxmoxResourceMapper.ToResources(scope, nodes, guests);
    }
}
