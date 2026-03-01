using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Templates;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Templates;

public class TemplateListSettings : CommandSettings
{
    [CommandOption("--kind|-k")]
    [Description("Filter templates by resource kind (e.g. Switch, Router, Firewall).")]
    public string? Kind { get; set; }
}

public class TemplateListCommand(IServiceProvider provider)
    : AsyncCommand<TemplateListSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TemplateListSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IHardwareTemplateStore>();

        var templates = string.IsNullOrWhiteSpace(settings.Kind)
            ? await store.GetAllAsync()
            : await store.GetAllByKindAsync(settings.Kind);

        if (templates.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No templates found.[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("Kind");
        table.AddColumn("Model");
        table.AddColumn("Template ID");

        foreach (var t in templates.OrderBy(t => t.Kind).ThenBy(t => t.Model))
            table.AddRow(t.Kind, t.Model, t.Id);

        AnsiConsole.Write(table);
        return 0;
    }
}
