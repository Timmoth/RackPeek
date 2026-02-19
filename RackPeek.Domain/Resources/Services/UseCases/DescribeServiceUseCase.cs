using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Services.UseCases;

public record ServiceDescription(
    string Name,
    string? Ip,
    int? Port,
    string? Protocol,
    string? Url,
    string? RunsOnSystemHost,
    string? RunsOnPhysicalHost
);

public class DescribeServiceUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<ServiceDescription> ExecuteAsync(string name)
    {
        name = Normalize.ServiceName(name);
        ThrowIfInvalid.ResourceName(name);
        var service = await repository.GetByNameAsync(name) as Service;
        if (service is null)
            throw new NotFoundException($"Service '{name}' not found.");

        string? runsOnPhysicalHost = null;
        if (!string.IsNullOrEmpty(service.RunsOn))
        {
            var systemResource = await repository.GetByNameAsync(service.RunsOn) as SystemResource;
            runsOnPhysicalHost = systemResource?.RunsOn;
        }

        return new ServiceDescription(
            service.Name,
            service.Network?.Ip,
            service.Network?.Port,
            service.Network?.Protocol,
            service.Network?.Url,
            service.RunsOn,
            runsOnPhysicalHost
        );
    }
}