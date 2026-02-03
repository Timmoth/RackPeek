using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class AddSystemUseCase(ISystemRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string name, string? runsOn = null)
    {
        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);
        
        var existingResourceKind = await resourceRepo.GetResourceKindAsync(name);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{name}' already exists.");

        if (!string.IsNullOrEmpty(runsOn))
        {
            runsOn = Normalize.HardwareName(runsOn);
            var parentResourceKind = await resourceRepo.GetResourceKindAsync(runsOn);
            if (string.IsNullOrEmpty(parentResourceKind))
                throw new NotFoundException($"Parent resource '{runsOn}' does not exist.");
            
            if (parentResourceKind is Service.KindLabel or SystemResource.KindLabel)
                throw new NotFoundException($"Parent resource '{runsOn}' must be hardware.");
        }
        
        var system = new SystemResource
        {
            Name = name,
            RunsOn = runsOn
        };

        await repository.AddAsync(system);
    }
}