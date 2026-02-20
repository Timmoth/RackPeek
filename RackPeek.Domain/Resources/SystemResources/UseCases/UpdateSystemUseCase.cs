using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Hardware;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class UpdateSystemUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? type = null,
        string? os = null,
        int? cores = null,
        int? ram = null,
        string? runsOn = null,
        string? notes = null
    )
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);


        var system = await repository.GetByNameAsync(name) as SystemResource;
        if (system is null)
            throw new InvalidOperationException($"System '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedSystemType = Normalize.SystemType(type);
            ThrowIfInvalid.SystemType(normalizedSystemType);
            system.Type = normalizedSystemType;
        }

        if (!string.IsNullOrWhiteSpace(os))
            system.Os = os;

        if (cores.HasValue)
            system.Cores = cores.Value;

        if (ram.HasValue)
            system.Ram = ram.Value;

        if (notes != null)
        {
            system.Notes = notes;
        }
        
        if (!string.IsNullOrWhiteSpace(runsOn))
        {
            ThrowIfInvalid.ResourceName(runsOn);
            var parentHardware = await repository.GetByNameAsync(runsOn) as Hardware.Hardware;
            if (parentHardware == null) throw new NotFoundException($"Parent hardware '{runsOn}' not found.");
            system.RunsOn = runsOn;
        }


        await repository.UpdateAsync(system);
    }
}