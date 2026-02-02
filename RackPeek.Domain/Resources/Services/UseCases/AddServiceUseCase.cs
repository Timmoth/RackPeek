using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class AddServiceUseCase(IServiceRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.ServiceName(name);
        ThrowIfInvalid.ResourceName(name);

        var existingResourceKind = await resourceRepo.GetResourceKindAsync(name);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{name}' already exists.");

        var service = new Service
        {
            Name = name
        };

        await repository.AddAsync(service);
    }
}