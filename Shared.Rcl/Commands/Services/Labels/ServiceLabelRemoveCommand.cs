using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Services.Labels;

public class ServiceLabelRemoveSettings : ServiceNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class ServiceLabelRemoveCommand(IServiceProvider serviceProvider) : AsyncCommand<ServiceLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ServiceLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<Service>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}