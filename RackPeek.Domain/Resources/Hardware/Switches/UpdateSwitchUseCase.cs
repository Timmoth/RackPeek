using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public class UpdateSwitchUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        bool? managed = null,
        bool? poe = null
    )
    {
        var switchResource = await repository.GetByNameAsync(name) as Switch;
        if (switchResource == null)
            throw new InvalidOperationException($"Switch '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            switchResource.Model = model;

        if (managed.HasValue)
            switchResource.Managed = managed.Value;

        if (poe.HasValue)
            switchResource.Poe = poe.Value;

        await repository.UpdateAsync(switchResource);
    }
}