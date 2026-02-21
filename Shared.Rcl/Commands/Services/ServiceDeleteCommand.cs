using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Services;

public class ServiceDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServiceNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServiceNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteResourceUseCase<Service>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Service '{settings.Name}' deleted.[/]");
        return 0;
    }
}