using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware;

public class GetHardwareSystemTreeUseCase(
    IHardwareRepository hardwareRepository,
    ISystemRepository systemRepository,
    IServiceRepository serviceRepository) : IUseCase
{
    public async Task<HardwareDependencyTree> ExecuteAsync(string hardwareName)
    {
        ThrowIfInvalid.ResourceName(hardwareName);

        var server = await hardwareRepository.GetByNameAsync(hardwareName);
        if (server is null) 
            throw new NotFoundException($"Hardware '{hardwareName}' not found.");

        return await BuildDependencyTreeAsync(server);
    }

    private async Task<HardwareDependencyTree> BuildDependencyTreeAsync(Models.Hardware server)
    {
        var systems = await systemRepository.GetByPhysicalHostAsync(server.Name);

        var systemTrees = new List<SystemDependencyTree>();
        foreach (var system in systems) systemTrees.Add(await BuildSystemDependencyTreeAsync(system));

        return new HardwareDependencyTree(server, systemTrees);
    }

    private async Task<SystemDependencyTree> BuildSystemDependencyTreeAsync(SystemResource system)
    {
        var services = await serviceRepository.GetBySystemHostAsync(system.Name);

        return new SystemDependencyTree(system, services);
    }
}

public sealed class HardwareDependencyTree(Models.Hardware hardware, IEnumerable<SystemDependencyTree> systems)
{
    public Models.Hardware Hardware { get; } = hardware;
    public IEnumerable<SystemDependencyTree> Systems { get; } = systems;
}

public sealed class SystemDependencyTree(SystemResource system, IEnumerable<Service> services)
{
    public SystemResource System { get; } = system;
    public IEnumerable<Service> Services { get; } = services;
}