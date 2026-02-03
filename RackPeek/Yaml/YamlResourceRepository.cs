using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Yaml;

public class YamlResourceRepository(YamlResourceCollection resources) : IResourceRepository
{
    public Task<string?> GetResourceKindAsync(string name)
    {
        // Use the centralized GetByName which handles casing correctly
        var resource = resources.GetByName(name);
        
        // Return the Kind label if it exists
        return Task.FromResult(resource switch
        {
            Hardware h => h.Kind,
            SystemResource s => SystemResource.KindLabel,
            Service svc => Service.KindLabel,
            _ => null
        });
    }

    public Task<bool> ResourceExistsAsync(string name)
    {
        return Task.FromResult(resources.GetByName(name) != null);
    }
}