using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware;

public class GetHardwareSystemTreeUseCase(
    IResourceCollection repo) : IUseCase
{
    public async Task<HardwareDependencyTree> ExecuteAsync(string hardwareName)
    {
        ThrowIfInvalid.ResourceName(hardwareName);

        var server = await repo.GetByNameAsync(hardwareName) as Hardware;
        if (server is null)
            throw new NotFoundException($"Hardware '{hardwareName}' not found.");

        return await BuildDependencyTreeAsync(server);
    }

    private async Task<HardwareDependencyTree> BuildDependencyTreeAsync(Hardware server)
    {
        var systems = await repo.GetDependantsAsync(server.Name);

        var systemTrees = new List<SystemDependencyTree>();
        foreach (var system in systems.OfType<SystemResource>()) systemTrees.Add(await BuildSystemDependencyTreeAsync(system));

        return new HardwareDependencyTree(server, systemTrees);
    }

    private async Task<SystemDependencyTree> BuildSystemDependencyTreeAsync(SystemResource system)
    {
        var services = await repo.GetDependantsAsync(system.Name);

        return new SystemDependencyTree(system, services.OfType<Service>());
    }
}

public sealed class HardwareDependencyTree(Hardware hardware, IEnumerable<SystemDependencyTree> systems)
{
    public Hardware Hardware { get; } = hardware;
    public IEnumerable<SystemDependencyTree> Systems { get; } = systems;
}

public sealed class SystemDependencyTree(SystemResource system, IEnumerable<Service> services)
{
    public SystemResource System { get; } = system;
    public IEnumerable<Service> Services { get; } = services;
}