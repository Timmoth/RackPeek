using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
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
    Task<Resource?> GetByNameAsync(string name);
    Task<T?> GetByNameAsync<T>(string name) where T : Resource;

    Resource? GetByName(string name);
    Task<bool> Exists(string name);
    
    Task<string?> GetKind(string? name);


    Task LoadAsync(); // required for WASM startup
    Task<IReadOnlyList<Resource>> GetByTagAsync(string name);
    public Task<Dictionary<string, int>> GetTagsAsync();
    
    Task<IReadOnlyList<(Resource, string)>> GetByLabelAsync(string name);
    public Task<Dictionary<string, int>> GetLabelsAsync();
    
    Task<IReadOnlyList<(Resource, string)>> GetResourceIpsAsync();

    Task<IReadOnlyList<T>> GetAllOfTypeAsync<T>();
    Task<IReadOnlyList<Resource>> GetDependantsAsync(string name);

    Task Merge(string incomingYaml, MergeMode mode);


}