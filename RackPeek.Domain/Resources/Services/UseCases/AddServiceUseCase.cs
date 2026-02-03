using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class AddServiceUseCase(IServiceRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string name, string? runsOn = null)
    {
        name = Normalize.ServiceName(name);
        ThrowIfInvalid.ResourceName(name);

        var existingResourceKind = await resourceRepo.GetResourceKindAsync(name);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{name}' already exists.");

        if (!string.IsNullOrEmpty(runsOn))
        {
            runsOn = Normalize.SystemName(runsOn);
            var parentResourceKind = await resourceRepo.GetResourceKindAsync(runsOn);
            if (string.IsNullOrEmpty(parentResourceKind))
                throw new ConflictException($"Parent resource '{runsOn}' does not exist.");
        }
        
        var service = new Service
        {
            Name = name,
            RunsOn = runsOn,
        };

        await repository.AddAsync(service);
    }
}