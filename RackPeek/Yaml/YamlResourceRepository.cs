using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Yaml;

public class YamlResourceRepository(YamlResourceCollection resources) : IResourceRepository
{
    public Task<string?> GetResourceKindAsync(string name)
    {
        var hardware = resources.HardwareResources.FirstOrDefault(r => r.Name == name);
        if (hardware != null)
        {
            return Task.FromResult<string?>(hardware.Kind);
        }
        var systemResource = resources.SystemResources.FirstOrDefault(r => r.Name == name);
        if (systemResource != null)
        {
            return Task.FromResult<string?>(systemResource.Kind);
        }
        var service = resources.ServiceResources.FirstOrDefault(r => r.Name == name);
        if (service != null)
        {
            return Task.FromResult<string?>(service.Kind);
        }
        
        return Task.FromResult<string?>(null);
    }

    public Task<bool> ResourceExistsAsync(string name)
    {
        var hardware = resources.HardwareResources.FirstOrDefault(r => r.Name == name);
        if (hardware != null)
        {
            return Task.FromResult(true);
        }
        var systemResource = resources.SystemResources.FirstOrDefault(r => r.Name == name);
        if (systemResource != null)
        {
            return Task.FromResult(true);
        }
        var service = resources.ServiceResources.FirstOrDefault(r => r.Name == name);
        if (service != null)
        {
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }
}