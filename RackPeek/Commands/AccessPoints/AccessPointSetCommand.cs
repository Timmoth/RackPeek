using Microsoft.Extensions.DependencyInjection;
using RackPeek.Commands.Server;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.AccessPoints;

public class AccessPointSetSettings : ServerNameSettings
{
    [CommandOption("--model")] 
    public string? Model { get; set; }

    [CommandOption("--speed")] 
    public double? Speed { get; set; }
}

public class AccessPointSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<AccessPointSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AccessPointSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateAccessPointUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Model,
            settings.Speed
        );

        AnsiConsole.MarkupLine($"[green]Access point '{settings.Name}' updated.[/]");
        return 0;
    }
}