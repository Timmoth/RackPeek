using Microsoft.Extensions.DependencyInjection;
using RackPeek.Commands.Servers;
using RackPeek.Domain.Resources.Hardware.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Ups;

public class UpsSetSettings : ServerNameSettings
{
    [CommandOption("--model")] public string? Model { get; set; }
    [CommandOption("--va")] public int? Va { get; set; }
}

public class UpsSetCommand(IServiceProvider provider)
    : AsyncCommand<UpsSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateUpsUseCase>();

        await useCase.ExecuteAsync(settings.Name, settings.Model, settings.Va);

        AnsiConsole.MarkupLine($"[green]UPS '{settings.Name}' updated.[/]");
        return 0;
    }
}