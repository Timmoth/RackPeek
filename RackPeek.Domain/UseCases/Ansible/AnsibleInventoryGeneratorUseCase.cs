using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Ansible;

public class AnsibleInventoryGeneratorUseCase(IResourceCollection repository) : IUseCase {
    public async Task<InventoryResult?> ExecuteAsync(InventoryOptions options) {
        IReadOnlyList<Resource> resources = await repository.GetAllOfTypeAsync<Resource>();
        return resources.ToAnsibleInventory(options);
    }
}
