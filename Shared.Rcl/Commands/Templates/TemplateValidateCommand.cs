using System.ComponentModel;
using RackPeek.Domain.Templates;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Templates;

/// <summary>
/// Settings for the <c>rpk templates validate</c> command.
/// </summary>
public class TemplateValidateSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Path to the YAML template file to validate.")]
    public string Path { get; set; } = default!;
}

/// <summary>
/// Validates a hardware template YAML file against the resource schema for its kind.
/// </summary>
public class TemplateValidateCommand : AsyncCommand<TemplateValidateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TemplateValidateSettings settings,
        CancellationToken cancellationToken)
    {
        var path = settings.Path;

        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]File not found:[/] {path}");
            return 1;
        }

        var yaml = await File.ReadAllTextAsync(path, cancellationToken);
        var fileName = System.IO.Path.GetFileName(path);

        var validator = new TemplateValidator();
        var errors = validator.Validate(yaml, fileName);

        if (errors.Count == 0)
        {
            AnsiConsole.MarkupLine($"[green]Valid:[/] {fileName} passes all checks.");
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]Invalid:[/] {Markup.Escape(fileName)} has {errors.Count} error(s):");
        foreach (var error in errors)
            AnsiConsole.MarkupLine($"  [yellow]•[/] {Markup.Escape(error)}");

        return 1;
    }
}
