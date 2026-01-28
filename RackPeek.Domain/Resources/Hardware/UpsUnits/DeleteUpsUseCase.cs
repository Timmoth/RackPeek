using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public class DeleteUpsUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        if (await repository.GetByNameAsync(name) is not Ups ups)
            throw new InvalidOperationException($"UPS '{name}' not found.");

        await repository.DeleteAsync(name);
    }
}