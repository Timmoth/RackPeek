using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Services;

public class ServiceGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServiceNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServiceNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeServiceUseCase>();

        var Service = await useCase.ExecuteAsync(settings.Name);

        if (Service == null)
        {
            AnsiConsole.MarkupLine($"[red]Service '{settings.Name}' not found.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"[green]{Service.Name}[/]  Ip: {Service.Ip ?? "Unknown"}, Port: {Service.Port.ToString() ?? "Unknown"}, Protocol: {Service.Protocol ?? "Unknown"}, Url: {Service.Url ?? "Unknown"}, RunsOn: {ServicesFormatExtensions.FormatRunsOn(Service.RunsOnSystemHost, Service.RunsOnPhysicalHost)}");
        return 0;
    }
}