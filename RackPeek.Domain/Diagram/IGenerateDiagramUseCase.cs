namespace RackPeek.Domain.Diagram;

public interface IGenerateDiagramUseCase
{
    Task<string> ExecuteAsync();
}