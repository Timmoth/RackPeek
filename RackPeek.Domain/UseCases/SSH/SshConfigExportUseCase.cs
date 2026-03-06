using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.SSH;

public class SshConfigExportUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<SshExportResult?> ExecuteAsync(SshExportOptions options)
    {
        IReadOnlyList<Resource> resources = await repository.GetAllOfTypeAsync<Resource>();
        return resources.ToSshConfig(options);
    }
}
