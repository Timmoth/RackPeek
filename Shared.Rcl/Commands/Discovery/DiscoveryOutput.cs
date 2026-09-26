using RackPeek.Domain.Api;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using Spectre.Console;

namespace Shared.Rcl.Commands.Discovery;

/// <summary>
///     The one place a discovery result leaves the process: printed as YAML, or sent to
///     a server. Shared so every collector behaves identically.
/// </summary>
public static class DiscoveryOutput {
    public static async Task<int> EmitAsync(
        IReadOnlyList<Resource> resources,
        DiscoverSettings settings,
        CancellationToken cancellationToken) {
        if (resources.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]Nothing discovered.[/]");

            return 0;
        }

        var yaml = DiscoveryDocument.ToYaml(resources);

        if (!settings.ShouldUpload) {
            // Raw write, not through Spectre's renderer: this is meant to be redirected
            // to a file, and the renderer word-wraps lines longer than the console
            // width — which splits a long single-token scalar and corrupts the YAML.
            // The active console's own writer keeps the web console emulator working.
            await AnsiConsole.Console.Profile.Out.Writer.WriteLineAsync(yaml);

            return 0;
        }

        return await UploadAsync(yaml, settings, cancellationToken);
    }

    private static async Task<int> UploadAsync(
        string yaml,
        DiscoverSettings settings,
        CancellationToken cancellationToken) {
        var server = settings.ResolvedServer;
        var apiKey = settings.ResolvedApiKey;

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(apiKey)) {
            // Validate() enforces both; this is the guard for a caller that skipped it.
            AnsiConsole.MarkupLine(
                "[red]No server or API key. Pass --server and --api-key, or set RPK_SERVER and RPK_API_KEY.[/]");

            return 1;
        }

        try {
            using var publisher = new DiscoveryPublisher(server, apiKey);

            ImportYamlResponse response = await publisher.PublishAsync(yaml, settings.DryRun, cancellationToken);

            Report(response, settings.DryRun, server);

            return 0;
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or HttpRequestException or UriFormatException
            // HttpClient reports its own timeout as a cancellation.
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)) {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");

            return 1;
        }
    }

    private static void Report(ImportYamlResponse response, bool dryRun, string server) {
        List(response.Added, "added", "green");
        List(response.Updated, "updated", "yellow");
        List(response.Replaced, "replaced", "yellow");

        var total = response.Added.Count + response.Updated.Count + response.Replaced.Count;

        if (total == 0) {
            AnsiConsole.MarkupLine($"[grey]No changes — {Markup.Escape(server)} is already up to date.[/]");

            return;
        }

        AnsiConsole.MarkupLine(dryRun
            ? $"[grey]Dry run — nothing was written to {Markup.Escape(server)}.[/]"
            : $"[grey]{total} resource(s) written to {Markup.Escape(server)}.[/]");
    }

    private static void List(IReadOnlyList<string> names, string label, string colour) {
        foreach (var name in names)
            AnsiConsole.MarkupLine($"[{colour}]{label}[/] {Markup.Escape(name)}");
    }
}
