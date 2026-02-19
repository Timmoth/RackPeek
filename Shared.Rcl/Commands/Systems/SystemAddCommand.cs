using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Systems;

public class SystemAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}

public class SystemAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SystemAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddResourceUseCase<SystemResource>>();

        await useCase.ExecuteAsync(
            settings.Name
        );

        AnsiConsole.MarkupLine($"[green]System '{settings.Name}' added.[/]");
        return 0;
    }
}