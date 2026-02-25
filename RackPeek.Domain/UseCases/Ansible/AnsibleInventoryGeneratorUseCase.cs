using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Ansible;

public class AnsibleInventoryGeneratorUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<InventoryResult?> ExecuteAsync(InventoryOptions options)
    {
        var resources = await repository.GetAllOfTypeAsync<Resource>();
        return resources.ToAnsibleInventoryIni(options);
    }
}