using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopGetCommand(IServiceProvider provider)
    : AsyncCommand {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        IGetAllResourcesByKindUseCase<Laptop> useCase =
            scope.ServiceProvider.GetRequiredService<IGetAllResourcesByKindUseCase<Laptop>>();

        IReadOnlyList<Laptop> laptops = await useCase.ExecuteAsync();

        if (laptops.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]No Laptops found.[/]");
            return 0;
        }

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("CPUs")
            .AddColumn("RAM")
            .AddColumn("Drives")
            .AddColumn("GPUs");

        foreach (Laptop d in laptops)
            table.AddRow(
                d.Name,
                (d.Cpus?.Count ?? 0).ToString(),
                d.Ram == null ? "None" : $"{d.Ram.Size}GB",
                (d.Drives?.Count ?? 0).ToString(),
                (d.Gpus?.Count ?? 0).ToString()
            );

        AnsiConsole.Write(table);
        return 0;
    }
}
