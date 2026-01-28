using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Commands.Servers;
using RackPeek.Domain.Resources.Services.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Services;

public class ServiceSetSettings : ServerNameSettings
{
    [CommandOption("--ip")]
    [Description("The ip address of the service.")]
    public string? Ip { get; set; }

    [CommandOption("--port")]
    [Description("The port the service is running on.")]
    public int? Port { get; set; }

    [CommandOption("--protocol")]
    [Description("The service protocol.")]
    public string? Protocol { get; set; }

    [CommandOption("--url")]
    [Description("The service URL.")]
    public string? Url { get; set; }

    [CommandOption("--runs-on")]
    [Description("The system the service is running on.")]
    public string? RunsOn { get; set; }
}

public class ServiceSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServiceSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServiceSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateServiceUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Ip,
            settings.Port,
            settings.Protocol,
            settings.Url,
            settings.RunsOn
        );

        AnsiConsole.MarkupLine($"[green]Service '{settings.Name}' updated.[/]");
        return 0;
    }
}