using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek;

public class YamlHardwareRepository(YamlResourceCollection resourceCollection) : IHardwareRepository
{
    public Task<IReadOnlyList<Hardware>> GetAllAsync()
    {
        return Task.FromResult(resourceCollection.HardwareResources);
    }
}