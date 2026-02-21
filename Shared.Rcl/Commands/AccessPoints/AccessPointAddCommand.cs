using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.AccessPoints;

public class AccessPointAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The access point name.")]
    public string Name { get; set; } = default!;
}

public class AccessPointAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<AccessPointAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AccessPointAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddResourceUseCase<AccessPoint>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Access Point '{settings.Name}' added.[/]");
        return 0;
    }
}