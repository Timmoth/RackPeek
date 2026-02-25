using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Systems;

public class SystemGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SystemNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeSystemUseCase>();

        var system = await useCase.ExecuteAsync(settings.Name);
        AnsiConsole.MarkupLine(
            $"[green]{system.Name}[/]  Type: {system.Type ?? "Unknown"}, OS: {system.Os ?? "Unknown"}, " +
            $"Cores: {system.Cores}, RAM: {system.RamGb}GB, Storage: {system.TotalStorageGb}GB, RunsOn: {string.Join(", ", system.RunsOn!)}");

        return 0;
    }
}