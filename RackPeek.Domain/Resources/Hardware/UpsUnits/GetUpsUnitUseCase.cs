using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public class GetUpsUnitUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Ups?> ExecuteAsync(string name)
    {
        var hardware = await repository.GetByNameAsync(name);
        return hardware as Ups;
    }
}