using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases.Mermaid;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Exporters;

public sealed class GenerateMermaidDiagramCommand(IServiceProvider provider)
    : AsyncCommand<GenerateMermaidDiagramSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        GenerateMermaidDiagramSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();

        MermaidDiagramExportUseCase useCase = scope.ServiceProvider
            .GetRequiredService<MermaidDiagramExportUseCase>();

        var options = new MermaidExportOptions {
            IncludeTags = ParseCsv(settings.IncludeTags),
            DiagramType = settings.DiagramType ?? "flowchart TD",
            IncludeLabels = !settings.NoLabels,
            IncludeEdges = !settings.NoEdges,
            LabelWhitelist = ParseCsv(settings.LabelWhitelist)
        };

        MermaidExportResult? result = await useCase.ExecuteAsync(options);

        if (result is null) {
            AnsiConsole.MarkupLine("[red]Mermaid export returned null.[/]");
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
                result.DiagramText,
                cancellationToken);

            AnsiConsole.MarkupLine(
                $"[green]Mermaid diagram written to:[/] {Markup.Escape(settings.OutputPath)}");
        }
        else {
            AnsiConsole.MarkupLine("[green]Generated Mermaid Diagram:[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(result.DiagramText);
        }

        return 0;
    }

    private static IReadOnlyList<string> ParseCsv(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }
}
