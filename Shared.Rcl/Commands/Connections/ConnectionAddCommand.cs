using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Connections;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Connections;

public class ConnectionAddSettings : CommandSettings {
    [CommandArgument(0, "<A_RESOURCE>")]
    [Description("Resource name for endpoint A.")]
    public string AResource { get; set; } = null!;

    [CommandArgument(1, "<A_GROUP>")]
    [Description("Port group index for endpoint A.")]
    public int AGroup { get; set; }

    [CommandArgument(2, "<A_INDEX>")]
    [Description("Port index for endpoint A.")]
    public int AIndex { get; set; }

    [CommandArgument(3, "<B_RESOURCE>")]
    [Description("Resource name for endpoint B.")]
    public string BResource { get; set; } = null!;

    [CommandArgument(4, "<B_GROUP>")]
    [Description("Port group index for endpoint B.")]
    public int BGroup { get; set; }

    [CommandArgument(5, "<B_INDEX>")]
    [Description("Port index for endpoint B.")]
    public int BIndex { get; set; }

    [CommandOption("--label")]
    [Description("Optional label for the connection.")]
    public string? Label { get; set; }

    [CommandOption("--notes")]
    [Description("Optional notes for the connection.")]
    public string? Notes { get; set; }
}

public class ConnectionAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ConnectionAddSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ConnectionAddSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();

        IAddConnectionUseCase useCase =
            scope.ServiceProvider.GetRequiredService<IAddConnectionUseCase>();

        var a = new PortReference {
            Resource = settings.AResource,
            PortGroup = settings.AGroup,
            PortIndex = settings.AIndex
        };

        var b = new PortReference {
            Resource = settings.BResource,
            PortGroup = settings.BGroup,
            PortIndex = settings.BIndex
        };

        await useCase.ExecuteAsync(
            a,
            b,
            settings.Label,
            settings.Notes
        );

        AnsiConsole.MarkupLine(
            $"[green]Connection created:[/] " +
            $"{settings.AResource}:{settings.AGroup}:{settings.AIndex} " +
            $"<-> " +
            $"{settings.BResource}:{settings.BGroup}:{settings.BIndex}"
        );

        return 0;
    }
}
