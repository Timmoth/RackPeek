using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Services;

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

        var service = await useCase.ExecuteAsync(settings.Name);

        var sys = string.Join(", ", service.RunsOnSystemHost);
        var phys = string.Join(", ", service.RunsOnPhysicalHost);

        AnsiConsole.MarkupLine(
            $"[green]{service.Name}[/]  Ip: {service.Ip ?? "Unknown"}, Port: {service.Port.ToString() ?? "Unknown"}, Protocol: {service.Protocol ?? "Unknown"}, Url: {service.Url ?? "Unknown"}, RunsOn: {ServicesFormatExtensions.FormatRunsOn(sys, phys)}");
        return 0;
    }
}
