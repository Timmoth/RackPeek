using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class UpdateServiceUseCase(IServiceRepository repository, ISystemRepository systemRepo) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? ip = null,
        int? port = null,
        string? protocol = null,
        string? url = null,
        string? runsOn = null
    )
    {
        name = Normalize.ServiceName(name);
        ThrowIfInvalid.ResourceName(name);
        var service = await repository.GetByNameAsync(name);
        if (service is null)
            throw new NotFoundException($"Service '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(ip))
        {
            service.Network ??= new Network();
            service.Network.Ip = ip;
        }

        if (!string.IsNullOrWhiteSpace(protocol))
        {
            service.Network ??= new Network();
            service.Network.Protocol = protocol;
        }

        if (!string.IsNullOrWhiteSpace(url))
        {
            service.Network ??= new Network();
            service.Network.Url = url;
        }

        if (port.HasValue)
        {
            service.Network ??= new Network();
            service.Network.Port = port.Value;
        }

        if (!string.IsNullOrWhiteSpace(runsOn))
        {
            ThrowIfInvalid.ResourceName(runsOn);
            var parentSystem = await systemRepo.GetByNameAsync(runsOn);
            if (parentSystem == null) throw new NotFoundException($"Parent system '{runsOn}' not found.");
            service.RunsOn = runsOn;
        }

        await repository.UpdateAsync(service);
    }
}