using System.Collections.Generic;
using System.Threading.Tasks;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.UseCases.Mermaid
{
    public class MermaidDiagramExportUseCase : IUseCase
    {
        private readonly IResourceCollection _repository;

        public MermaidDiagramExportUseCase(IResourceCollection repository)
        {
            _repository = repository;
        }

        public async Task<MermaidExportResult?> ExecuteAsync(MermaidExportOptions options)
        {
            IReadOnlyList<Resource> resources = await _repository.GetAllOfTypeAsync<Resource>();
            return resources.ToMermaidDiagram(options);
        }
    }
}
