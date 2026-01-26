using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.AccessPoints;

public class AccessPointAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] 
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
        var useCase = scope.ServiceProvider.GetRequiredService<AddAccessPointUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Access point '{settings.Name}' added.[/]");
        return 0;
    }
}