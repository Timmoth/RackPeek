using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches;

public class SwitchDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SwitchNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SwitchNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteResourceUseCase<Switch>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Switch '{settings.Name}' deleted.[/]");
        return 0;
    }
}