using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Cpus;

public class DesktopCpuSetSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the desktop cpu.")]
    public int Index { get; set; }

    [CommandOption("--model")]
    [Description("The cpu model.")]
    public string? Model { get; set; }

    [CommandOption("--cores")]
    [Description("The number of cpu cores.")]
    public int? Cores { get; set; }

    [CommandOption("--threads")]
    [Description("The number of cpu threads.")]
    public int? Threads { get; set; }
}

public class DesktopCpuSetCommand(IServiceProvider provider) : AsyncCommand<DesktopCpuSetSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DesktopCpuSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateCpuUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index, settings.Model, settings.Cores,
            settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} updated on desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}