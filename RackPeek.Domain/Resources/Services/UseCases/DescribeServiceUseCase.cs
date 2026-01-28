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

public class DescribeServiceUseCase(IServiceRepository repository, ISystemRepository systemRepo) : IUseCase
{
    public async Task<ServiceDescription?> ExecuteAsync(string name)
    {
        var service = await repository.GetByNameAsync(name);
        if (service is null)
            return null;

        string? runsOnPhysicalHost = null;
        if (!string.IsNullOrEmpty(service.RunsOn))
        {
            var systemResource = await systemRepo.GetByNameAsync(service.RunsOn);
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