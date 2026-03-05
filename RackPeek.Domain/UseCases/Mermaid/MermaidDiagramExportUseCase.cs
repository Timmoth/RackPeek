using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Mermaid;

public class MermaidDiagramExportUseCase(IResourceCollection repository) : IUseCase {
    public async Task<MermaidExportResult?> ExecuteAsync(MermaidExportOptions options) {
        IReadOnlyList<Resource> resources = await repository.GetAllOfTypeAsync<Resource>();
        return resources.ToMermaidDiagram(options);
    }
}
