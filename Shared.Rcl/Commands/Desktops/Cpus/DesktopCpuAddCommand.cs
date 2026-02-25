using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Cpus;

public class DesktopCpuAddSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandOption("--model")]
    [Description("The model name.")]
    public string? Model { get; set; }

    [CommandOption("--cores")]
    [Description("The number of cpu cores.")]
    public int? Cores { get; set; }

    [CommandOption("--threads")]
    [Description("The number of cpu threads.")]
    public int? Threads { get; set; }
}

public class DesktopCpuAddCommand(IServiceProvider provider)
    : AsyncCommand<DesktopCpuAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopCpuAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddCpuUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Model, settings.Cores, settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU added to desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}