using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public class GetAccessPointUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<AccessPoint?> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var hardware = await repository.GetByNameAsync(name);
        return hardware as AccessPoint;
    }
}