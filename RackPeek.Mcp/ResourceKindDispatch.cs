using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.OtherHardware;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.UpsUnits;
using RackPeek.Domain.UseCases;

namespace RackPeek.Mcp;

/// <summary>
///     Closes the per-kind generic use cases over the concrete resource type at
///     runtime. MCP tools receive a name, not a type — the kind is looked up and the
///     call dispatched so type-sensitive code (the clone's deep copy serialises the
///     real derived type, not the abstract base) runs against the right generic.
/// </summary>
internal static class ResourceKindDispatch {
    public static async Task CloneAsync(
        IServiceProvider services,
        IResourceCollection repo,
        string originalName,
        string cloneName) {
        var kind = await repo.GetKind(originalName)
                   ?? throw new NotFoundException($"Resource '{originalName}' not found.");

        await (kind.Trim().ToLowerInvariant() switch {
            "server" => Run<Server>(),
            "switch" => Run<Switch>(),
            "firewall" => Run<Firewall>(),
            "router" => Run<Router>(),
            "accesspoint" => Run<AccessPoint>(),
            "desktop" => Run<Desktop>(),
            "laptop" => Run<Laptop>(),
            "ups" => Run<Ups>(),
            "other" => Run<Other>(),
            "system" => Run<SystemResource>(),
            "service" => Run<Service>(),
            _ => throw new NotFoundException($"Resource kind '{kind}' cannot be cloned.")
        });

        Task Run<T>() where T : Resource =>
            services.GetRequiredService<ICloneResourceUseCase<T>>()
                .ExecuteAsync(originalName, cloneName);
    }
}
