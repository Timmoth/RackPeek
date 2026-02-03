using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public class GetUpsUnitUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Ups> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Ups ups)
        {
            throw new NotFoundException($"Ups '{name}' not found.");
        }
        return ups;
    }
}