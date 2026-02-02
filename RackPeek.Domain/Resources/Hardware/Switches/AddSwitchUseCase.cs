using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public class AddSwitchUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        // basic guard rails
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new InvalidOperationException($"Switch '{name}' already exists.");

        var switchResource = new Switch
        {
            Name = name
        };

        await repository.AddAsync(switchResource);
    }
}