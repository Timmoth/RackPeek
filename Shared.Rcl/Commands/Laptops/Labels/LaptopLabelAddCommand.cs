using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Labels;

public class LaptopLabelAddSettings : LaptopNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class LaptopLabelAddCommand(IServiceProvider serviceProvider) : AsyncCommand<LaptopLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, LaptopLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<Laptop>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}