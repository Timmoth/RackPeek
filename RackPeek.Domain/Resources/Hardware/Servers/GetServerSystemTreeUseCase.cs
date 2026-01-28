using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class GetServerSystemTreeUseCase(
    IHardwareRepository hardwareRepository,
    ISystemRepository systemRepository,
    IServiceRepository serviceRepository)
{
    public async Task<HardwareDependencyTree?> ExecuteAsync(string hardwareName)
    {
        var server = await hardwareRepository.GetByNameAsync(hardwareName) as Server;
        if (server is null) return null;

        return await BuildDependencyTreeAsync(server);
    }

    private async Task<HardwareDependencyTree> BuildDependencyTreeAsync(Server server)
    {
        var systems = await systemRepository.GetByPhysicalHostAsync(server.Name);

        var systemTrees = new List<SystemDependencyTree>();
        foreach (var system in systems)
        {
            systemTrees.Add(await BuildSystemDependencyTreeAsync(system));
        }

        return new HardwareDependencyTree(server, systemTrees);
    }

    private async Task<SystemDependencyTree> BuildSystemDependencyTreeAsync(SystemResource system)
    {
        var services = await serviceRepository.GetBySystemHostAsync(system.Name);

        return new SystemDependencyTree(system, services);
    }
}

public sealed class HardwareDependencyTree(Server hardware, IEnumerable<SystemDependencyTree> systems)
{
    public Server Hardware { get; } = hardware;
    public IEnumerable<SystemDependencyTree> Systems { get; } = systems;
}

public sealed class SystemDependencyTree(SystemResource system, IEnumerable<Service> services)
{
    public SystemResource System { get; } = system;
    public IEnumerable<Service> Services { get; } = services;
}
