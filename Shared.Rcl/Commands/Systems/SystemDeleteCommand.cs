using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Systems;

public class SystemDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SystemNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteResourceUseCase<SystemResource>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]System '{settings.Name}' deleted.[/]");
        return 0;
    }
}