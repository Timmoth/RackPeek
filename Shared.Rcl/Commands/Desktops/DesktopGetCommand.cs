using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops;

public class DesktopGetCommand(IServiceProvider provider)
    : AsyncCommand
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetAllResourcesByKindUseCase<Desktop>>();
   
        var desktops = await useCase.ExecuteAsync();

        if (desktops.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No desktops found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Model")
            .AddColumn("CPUs")
            .AddColumn("RAM")
            .AddColumn("Drives")
            .AddColumn("NICs")
            .AddColumn("GPUs");

        foreach (var d in desktops)
            table.AddRow(
                d.Name,
                d.Model ?? "Unknown",
                (d.Cpus?.Count ?? 0).ToString(),
                d.Ram == null ? "None" : $"{d.Ram.Size}GB",
                (d.Drives?.Count ?? 0).ToString(),
                (d.Nics?.Count ?? 0).ToString(),
                (d.Gpus?.Count ?? 0).ToString()
            );

        AnsiConsole.Write(table);
        return 0;
    }
}