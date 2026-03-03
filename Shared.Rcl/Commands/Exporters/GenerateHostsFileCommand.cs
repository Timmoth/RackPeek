using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases.Hosts;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Exporters;

public sealed class GenerateHostsFileCommand(IServiceProvider provider)
    : AsyncCommand<GenerateHostsFileSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        GenerateHostsFileSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();

        HostsFileExportUseCase useCase = scope.ServiceProvider
            .GetRequiredService<HostsFileExportUseCase>();

        var options = new HostsExportOptions {
            IncludeTags = ParseCsv(settings.IncludeTags),
            DomainSuffix = settings.DomainSuffix,
            IncludeLocalhostDefaults = !settings.NoLocalhost
        };

        HostsExportResult? result = await useCase.ExecuteAsync(options);

        if (result is null) {
            AnsiConsole.MarkupLine("[red]Hosts export returned null.[/]");
            return -1;
        }

        if (result.Warnings.Any()) {
            AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
            foreach (var warning in result.Warnings)
                AnsiConsole.MarkupLine($"[yellow]- {Markup.Escape(warning)}[/]");
            AnsiConsole.WriteLine();
        }

        if (!string.IsNullOrWhiteSpace(settings.OutputPath)) {
            await File.WriteAllTextAsync(
                settings.OutputPath,
                result.HostsText,
                cancellationToken);

            AnsiConsole.MarkupLine(
                $"[green]Hosts file written to:[/] {Markup.Escape(settings.OutputPath)}");
        }
        else {
            AnsiConsole.MarkupLine("[green]Generated Hosts File:[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(result.HostsText);
        }

        return 0;
    }

    private static IReadOnlyList<string> ParseCsv(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }
}
