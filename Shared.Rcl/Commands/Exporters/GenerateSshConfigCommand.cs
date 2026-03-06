using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases.SSH;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Exporters;

public sealed class GenerateSshConfigCommand(IServiceProvider provider)
    : AsyncCommand<GenerateSshConfigSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        GenerateSshConfigSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();

        SshConfigExportUseCase useCase = scope.ServiceProvider
            .GetRequiredService<SshConfigExportUseCase>();

        var options = new SshExportOptions {
            IncludeTags = ParseCsv(settings.IncludeTags),
            DefaultUser = settings.DefaultUser,
            DefaultPort = settings.DefaultPort,
            DefaultIdentityFile = settings.DefaultIdentityFile
        };

        SshExportResult? result = await useCase.ExecuteAsync(options);

        if (result is null) {
            AnsiConsole.MarkupLine("[red]SSH export returned null.[/]");
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
                result.ConfigText,
                cancellationToken);

            AnsiConsole.MarkupLine(
                $"[green]SSH config written to:[/] {Markup.Escape(settings.OutputPath)}");
        }
        else {
            AnsiConsole.MarkupLine("[green]Generated SSH Config:[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(result.ConfigText);
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
