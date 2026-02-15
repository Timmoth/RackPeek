using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Commands.Servers;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.AccessPoints;

public class AccessPointSetSettings : ServerNameSettings
{
    [CommandOption("--model")]
    [Description("The access point model name.")]
    public string? Model { get; set; }

    [CommandOption("--speed")]
    [Description("Speed in Gb/s (e.g. 1, 2.5, 10).")]
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

        AnsiConsole.MarkupLine($"[green]Access Point '{settings.Name}' updated.[/]");
        return 0;
    }
}