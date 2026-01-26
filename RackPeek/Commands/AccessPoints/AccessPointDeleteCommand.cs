using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.AccessPoints;

public class AccessPointDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<AccessPointNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AccessPointNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DeleteAccessPointUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Access point '{settings.Name}' deleted.[/]");
        return 0;
    }
}