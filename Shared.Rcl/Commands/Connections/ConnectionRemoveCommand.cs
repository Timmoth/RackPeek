using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Connections;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Connections;

public class ConnectionRemoveSettings : CommandSettings {
    [CommandArgument(0, "<RESOURCE>")]
    [Description("Resource name.")]
    public string Resource { get; set; } = null!;

    [CommandArgument(1, "<PORT_GROUP>")]
    [Description("Port group index.")]
    public int PortGroup { get; set; }

    [CommandArgument(2, "<PORT_INDEX>")]
    [Description("Port index.")]
    public int PortIndex { get; set; }
}

public class ConnectionRemoveCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ConnectionRemoveSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ConnectionRemoveSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();

        IRemoveConnectionUseCase useCase =
            scope.ServiceProvider.GetRequiredService<IRemoveConnectionUseCase>();

        var port = new PortReference {
            Resource = settings.Resource,
            PortGroup = settings.PortGroup,
            PortIndex = settings.PortIndex
        };

        await useCase.ExecuteAsync(port);

        AnsiConsole.MarkupLine(
            $"[green]Connection removed from[/] " +
            $"{settings.Resource}:{settings.PortGroup}:{settings.PortIndex}"
        );

        return 0;
    }
}
