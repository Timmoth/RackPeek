using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public class GetAccessPointsUseCase(IHardwareRepository repository)
{
    public async Task<IReadOnlyList<AccessPoint>> ExecuteAsync()
    {
        var hardware = await repository.GetAllAsync();
        return hardware.OfType<AccessPoint>().ToList();
    }
}