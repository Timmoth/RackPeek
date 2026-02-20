using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public class UpdateSwitchUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        bool? managed = null,
        bool? poe = null,
        string? notes = null
    )
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var switchResource = await repository.GetByNameAsync(name) as Switch;
        if (switchResource == null)
            throw new InvalidOperationException($"Switch '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            switchResource.Model = model;

        if (managed.HasValue)
            switchResource.Managed = managed.Value;

        if (poe.HasValue)
            switchResource.Poe = poe.Value;
        if (notes != null)
        {
            switchResource.Notes = notes;
        }
        await repository.UpdateAsync(switchResource);
    }
}