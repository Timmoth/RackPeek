using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services.UseCases;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands;

public class GetTotalSummaryCommand(IServiceProvider provider) : AsyncCommand {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();

        GetSystemSummaryUseCase systemUseCase =
            scope.ServiceProvider.GetRequiredService<GetSystemSummaryUseCase>();
        GetServiceSummaryUseCase serviceUseCase =
            scope.ServiceProvider.GetRequiredService<GetServiceSummaryUseCase>();
        GetHardwareUseCaseSummary hardwareUseCase =
            scope.ServiceProvider.GetRequiredService<GetHardwareUseCaseSummary>();

        // Execute all summaries in parallel
        Task<SystemSummary> systemTask = systemUseCase.ExecuteAsync();
        Task<AllServicesSummary> serviceTask = serviceUseCase.ExecuteAsync();
        Task<HardwareSummary> hardwareTask = hardwareUseCase.ExecuteAsync();

        await Task.WhenAll(systemTask, serviceTask, hardwareTask);

        SystemSummary systemSummary = systemTask.Result;
        AllServicesSummary serviceSummary = serviceTask.Result;
        HardwareSummary hardwareSummary = hardwareTask.Result;

        RenderSummaryTree(systemSummary, serviceSummary, hardwareSummary);

        return 0;
    }

    private static void RenderSummaryTree(
        SystemSummary systemSummary,
        AllServicesSummary serviceSummary,
        HardwareSummary hardwareSummary) {
        var tree = new Tree("[bold]Breakdown[/]");

        TreeNode hardwareNode = tree.AddNode(
            $"[bold]Hardware[/] ({hardwareSummary.TotalHardware})");

        foreach ((var kind, var count) in hardwareSummary.HardwareByKind.OrderByDescending(h => h.Value).ThenBy(h => h.Key))
            hardwareNode.AddNode($"{kind}: {count}");

        TreeNode systemsNode = tree.AddNode(
            $"[bold]Systems[/] ({systemSummary.TotalSystems})");

        if (systemSummary.SystemsByType.Count > 0) {
            TreeNode typesNode = systemsNode.AddNode("[bold]Types[/]");
            foreach ((var type, var count) in systemSummary.SystemsByType.OrderByDescending(h => h.Value)
                         .ThenBy(h => h.Key))
                typesNode.AddNode($"{type}: {count}");
        }

        if (systemSummary.SystemsByOs.Count > 0) {
            TreeNode osNode = systemsNode.AddNode("[bold]Operating Systems[/]");
            foreach ((var os, var count) in systemSummary.SystemsByOs.OrderByDescending(h => h.Value).ThenBy(h => h.Key))
                osNode.AddNode($"{os}: {count}");
        }

        TreeNode servicesNode = tree.AddNode(
            $"[bold]Services[/] ({serviceSummary.TotalServices})");

        servicesNode.AddNode(
            $"IP Addresses: {serviceSummary.TotalIpAddresses}");

        AnsiConsole.Write(tree);
    }

    private static void RenderTotals(
        SystemSummary systemSummary,
        AllServicesSummary serviceSummary,
        HardwareSummary hardwareSummary) {
        Grid grid = new Grid()
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

    private static void RenderSystemBreakdown(SystemSummary systemSummary) {
        if (systemSummary.SystemsByType.Count == 0 &&
            systemSummary.SystemsByOs.Count == 0)
            return;

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Systems Breakdown[/]")
            .AddColumn("Category")
            .AddColumn("Name")
            .AddColumn("Count");

        foreach ((var type, var count) in systemSummary.SystemsByType)
            table.AddRow("Type", type, count.ToString());

        foreach ((var os, var count) in systemSummary.SystemsByOs)
            table.AddRow("OS", os, count.ToString());

        AnsiConsole.Write(table);
    }

    private static void RenderHardwareBreakdown(HardwareSummary hardwareSummary) {
        if (hardwareSummary.HardwareByKind.Count == 0)
            return;

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Hardware Breakdown[/]")
            .AddColumn("Kind")
            .AddColumn("Count");

        foreach ((var kind, var count) in hardwareSummary.HardwareByKind)
            table.AddRow(kind, count.ToString());

        AnsiConsole.Write(table);
    }
}
