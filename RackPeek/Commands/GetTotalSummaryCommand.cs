using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services.UseCases;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands;

public class GetTotalSummaryCommand(IServiceProvider provider) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();

        var systemUseCase =
            scope.ServiceProvider.GetRequiredService<GetSystemSummaryUseCase>();
        var serviceUseCase =
            scope.ServiceProvider.GetRequiredService<GetServiceSummaryUseCase>();
        var hardwareUseCase =
            scope.ServiceProvider.GetRequiredService<GetHardwareUseCaseSummary>();

        // Execute all summaries in parallel
        var systemTask = systemUseCase.ExecuteAsync();
        var serviceTask = serviceUseCase.ExecuteAsync();
        var hardwareTask = hardwareUseCase.ExecuteAsync();

        await Task.WhenAll(systemTask, serviceTask, hardwareTask);

        var systemSummary = systemTask.Result;
        var serviceSummary = serviceTask.Result;
        var hardwareSummary = hardwareTask.Result;

        RenderSummaryTree(systemSummary, serviceSummary, hardwareSummary);

        return 0;
    }

    private static void RenderSummaryTree(
        SystemSummary systemSummary,
        AllServicesSummary serviceSummary,
        HardwareSummary hardwareSummary)
    {
        var tree = new Tree("[bold]Breakdown[/]");

        var hardwareNode = tree.AddNode(
            $"[bold]Hardware[/] ({hardwareSummary.TotalHardware})");

        foreach (var (kind, count) in hardwareSummary.HardwareByKind.OrderByDescending(h => h.Value).ThenBy(h => h.Key))
            hardwareNode.AddNode($"{kind}: {count}");

        var systemsNode = tree.AddNode(
            $"[bold]Systems[/] ({systemSummary.TotalSystems})");

        if (systemSummary.SystemsByType.Count > 0)
        {
            var typesNode = systemsNode.AddNode("[bold]Types[/]");
            foreach (var (type, count) in systemSummary.SystemsByType.OrderByDescending(h => h.Value)
                         .ThenBy(h => h.Key))
                typesNode.AddNode($"{type}: {count}");
        }

        if (systemSummary.SystemsByOs.Count > 0)
        {
            var osNode = systemsNode.AddNode("[bold]Operating Systems[/]");
            foreach (var (os, count) in systemSummary.SystemsByOs.OrderByDescending(h => h.Value).ThenBy(h => h.Key))
                osNode.AddNode($"{os}: {count}");
        }

        var servicesNode = tree.AddNode(
            $"[bold]Services[/] ({serviceSummary.TotalServices})");

        servicesNode.AddNode(
            $"IP Addresses: {serviceSummary.TotalIpAddresses}");

        AnsiConsole.Write(tree);
    }

    private static void RenderTotals(
        SystemSummary systemSummary,
        AllServicesSummary serviceSummary,
        HardwareSummary hardwareSummary)
    {
        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("[bold]Systems[/]", systemSummary.TotalSystems.ToString());
        grid.AddRow("[bold]Services[/]", serviceSummary.TotalServices.ToString());
        grid.AddRow("[bold]Service IPs[/]", serviceSummary.TotalIpAddresses.ToString());
        grid.AddRow("[bold]Hardware[/]", hardwareSummary.TotalHardware.ToString());

        AnsiConsole.Write(
            new Panel(grid)
                .Header("[bold]Totals[/]")
                .Border(BoxBorder.Rounded));
    }

    private static void RenderSystemBreakdown(SystemSummary systemSummary)
    {
        if (systemSummary.SystemsByType.Count == 0 &&
            systemSummary.SystemsByOs.Count == 0)
            return;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Systems Breakdown[/]")
            .AddColumn("Category")
            .AddColumn("Name")
            .AddColumn("Count");

        foreach (var (type, count) in systemSummary.SystemsByType)
            table.AddRow("Type", type, count.ToString());

        foreach (var (os, count) in systemSummary.SystemsByOs)
            table.AddRow("OS", os, count.ToString());

        AnsiConsole.Write(table);
    }

    private static void RenderHardwareBreakdown(HardwareSummary hardwareSummary)
    {
        if (hardwareSummary.HardwareByKind.Count == 0)
            return;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Hardware Breakdown[/]")
            .AddColumn("Kind")
            .AddColumn("Count");

        foreach (var (kind, count) in hardwareSummary.HardwareByKind)
            table.AddRow(kind, count.ToString());

        AnsiConsole.Write(table);
    }
}