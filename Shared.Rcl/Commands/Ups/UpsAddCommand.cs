using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Ups;

public class UpsAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}

public class UpsAddCommand(IServiceProvider provider)
    : AsyncCommand<UpsAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddUpsUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]UPS '{settings.Name}' added.[/]");
        return 0;
    }
}