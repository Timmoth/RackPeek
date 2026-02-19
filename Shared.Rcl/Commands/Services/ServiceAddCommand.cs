using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Services.UseCases;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Services;

public class ServiceAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The name of the service.")]
    public string Name { get; set; } = default!;
}

public class ServiceAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServiceAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServiceAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddResourceUseCase<Service>>();

        await useCase.ExecuteAsync(
            settings.Name
        );

        AnsiConsole.MarkupLine($"[green]Service '{settings.Name}' added.[/]");
        return 0;
    }
}