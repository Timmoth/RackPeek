using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Services.Labels;

public class ServiceLabelAddSettings : ServiceNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class ServiceLabelAddCommand(IServiceProvider serviceProvider) : AsyncCommand<ServiceLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ServiceLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<Service>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}