using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using RackPeek.Domain.Diagram;
using RackPeek.Domain.Diagram.UseCases;

namespace RackPeek.Commands.Diagram;

public class DiagramGenerateCommand(IServiceProvider serviceProvider) : AsyncCommand<DiagramGenerateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DiagramGenerateSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<GenerateDiagramUseCase>();
        
        var xml = await useCase.ExecuteAsync();
        await File.WriteAllTextAsync(settings.Output, xml, cancellationToken);
        return 0;
    }
}