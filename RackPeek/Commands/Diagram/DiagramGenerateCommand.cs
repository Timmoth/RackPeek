using Spectre.Console.Cli;
using RackPeek.Domain.Diagram;

namespace RackPeek.Commands.Diagram;

public class DiagramGenerateCommand : AsyncCommand<DiagramGenerateSettings>
{
    private readonly IGenerateDiagramUseCase _useCase;

    public DiagramGenerateCommand(IGenerateDiagramUseCase useCase)
    {
        _useCase = useCase;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DiagramGenerateSettings settings,
        CancellationToken cancellationToken)
    {
        var xml = await _useCase.ExecuteAsync();
        File.WriteAllText(settings.Output, xml);
        return 0;
    }
}