using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Persistence;

public interface IResourceCollection
{
    IReadOnlyList<Hardware> HardwareResources { get; }
    IReadOnlyList<SystemResource> SystemResources { get; }
    IReadOnlyList<Service> ServiceResources { get; }

    Task AddAsync(Resource resource);
    Task UpdateAsync(Resource resource);
    Task DeleteAsync(string name);

    Resource? GetByName(string name);
    Task<bool> Exists(string name);

    Task LoadAsync();   // required for WASM startup
    Task<IReadOnlyList<Resource>> GetByTagAsync(string name);
    public Task<Dictionary<string, int>> GetTagsAsync();

}