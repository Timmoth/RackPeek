using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Switches;

public class DeleteSwitchUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(string name)
    {
        if (await repository.GetByNameAsync(name) is not Switch hardware)
            throw new InvalidOperationException($"Switch '{name}' not found.");

        await repository.DeleteAsync(name);
    }
}