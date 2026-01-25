using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek;

public class YamlSystemRepository(YamlResourceCollection resourceCollection) : ISystemRepository
{
    public Task<IReadOnlyList<SystemResource>> GetAllAsync()
    {
        return Task.FromResult(resourceCollection.SystemResources);
    }
}