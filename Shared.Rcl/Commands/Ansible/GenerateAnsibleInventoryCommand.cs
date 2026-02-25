using RackPeek.Domain.Resources.Desktops;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using RackPeek.Domain.UseCases.Ansible;

namespace Shared.Rcl.Commands.Ansible;

public sealed class GenerateAnsibleInventorySettings : CommandSettings
{
    [CommandOption("--group-tags")]
    [Description("Comma-separated list of tags to group by (e.g. prod,staging)")]
    public string? GroupTags { get; init; }

    [CommandOption("--group-labels")]
    [Description("Comma-separated list of label keys to group by (e.g. env,site)")]
    public string? GroupLabels { get; init; }

    [CommandOption("--global-var")]
    [Description("Global variable (repeatable). Format: key=value")]
    public string[] GlobalVars { get; init; } = [];

    [CommandOption("-o|--output")]
    [Description("Write inventory to file instead of stdout")]
    public string? OutputPath { get; init; }
}


public sealed class GenerateAnsibleInventoryCommand(IServiceProvider provider)
    : AsyncCommand<GenerateAnsibleInventorySettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        GenerateAnsibleInventorySettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();

        var useCase = scope.ServiceProvider
            .GetRequiredService<AnsibleInventoryGeneratorUseCase>();

        var options = new InventoryOptions
        {
            GroupByTags = ParseCsv(settings.GroupTags),
            GroupByLabelKeys = ParseCsv(settings.GroupLabels),
            GlobalVars = ParseGlobalVars(settings.GlobalVars),
        };

        var result = await useCase.ExecuteAsync(options);

        if (result is null)
        {
            AnsiConsole.MarkupLine("[red]Inventory generation returned null.[/]");
            return -1;
        }

        if (result.Warnings.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
            foreach (var warning in result.Warnings)
                AnsiConsole.MarkupLine($"[yellow]- {Markup.Escape(warning)}[/]");
            AnsiConsole.WriteLine();
        }

        if (!string.IsNullOrWhiteSpace(settings.OutputPath))
        {
            await File.WriteAllTextAsync(settings.OutputPath, result.InventoryText, cancellationToken);

            AnsiConsole.MarkupLine(
                $"[green]Inventory written to:[/] {Markup.Escape(settings.OutputPath)}");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Generated Inventory:[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(result.InventoryText);
        }

        return 0;
    }

    // ------------------------

    private static IReadOnlyList<string> ParseCsv(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => x.Trim())
                  .Where(x => !string.IsNullOrWhiteSpace(x))
                  .ToArray();
    }

    private static IDictionary<string, string> ParseGlobalVars(string[] vars)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in vars ?? [])
        {
            var parts = entry.Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (!string.IsNullOrWhiteSpace(key))
                dict[key] = value;
        }

        return dict;
    }
}