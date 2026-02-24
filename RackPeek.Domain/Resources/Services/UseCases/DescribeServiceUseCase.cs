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
    List<string> RunsOnSystemHost,
    List<string> RunsOnPhysicalHost,
    Dictionary<string, string> Labels
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

        List<string> runsOnPhysicalHost = new List<string>();
        foreach (var systemName in service.RunsOn)
        {
            var systemResource = await repository.GetByNameAsync(systemName) as SystemResource;
            if (systemResource is not null)
            {
                foreach(var physicalName in systemResource.RunsOn)
                {
                    if (!runsOnPhysicalHost.Contains(physicalName))
                    {
                        runsOnPhysicalHost.Add(physicalName);
                    }
                }
            }
        }

        return new ServiceDescription(
            service.Name,
            service.Network?.Ip,
            service.Network?.Port,
            service.Network?.Protocol,
            service.Network?.Url,
            service.RunsOn,
            runsOnPhysicalHost,
            service.Labels
        );
    }
}
