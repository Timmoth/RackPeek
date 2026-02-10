using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Ups;

public class UpsNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}

public class UpsDeleteCommand(IServiceProvider provider)
    : AsyncCommand<UpsNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DeleteUpsUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]UPS '{settings.Name}' deleted.[/]");
        return 0;
    }
}