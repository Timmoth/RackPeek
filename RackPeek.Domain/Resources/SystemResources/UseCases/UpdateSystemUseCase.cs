using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class UpdateSystemUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? type = null,
        string? os = null,
        int? cores = null,
        double? ram = null,
        string? ip = null,
        List<string>? runsOn = null,
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

        if (ip != null)
        {
            system.Ip = ip;
        }
        
        if (notes != null) system.Notes = notes;

        if (runsOn?.Count > 0)
        {
            foreach(string parent in runsOn) {
                if (!string.IsNullOrWhiteSpace(parent)) {
                    ThrowIfInvalid.ResourceName(parent);
                    var parentHardware = await repository.GetByNameAsync(parent);

                    
                    if (parentHardware == null) throw new NotFoundException($"Parent '{parent}' not found.");
                    if (parentHardware is not Hardware.Hardware and not SystemResource)
                    {
                        throw new Exception("System cannot run on this resource.");
                    }
                    
                    if (!system.RunsOn.Contains(parent)) system.RunsOn.Add(parent);

                }
            }
        }

        await repository.UpdateAsync(system);
    }
}
