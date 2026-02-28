using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class UpdateServiceUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? ip = null,
        int? port = null,
        string? protocol = null,
        string? url = null,
        List<string>? runsOn = null,
        string? notes = null
    )
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.ServiceName(name);
        ThrowIfInvalid.ResourceName(name);
        var service = await repository.GetByNameAsync(name) as Service;
        if (service is null)
            throw new NotFoundException($"Service '{name}' not found.");

        if (ip != null)
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

        if (runsOn is not null)
        {
            var normalizedParents = new List<string>();

            foreach (var parent in runsOn
                         .Where(p => !string.IsNullOrWhiteSpace(p))
                         .Select(p => p.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ThrowIfInvalid.ResourceName(parent);

                var parentSystem = await repository.GetByNameAsync(parent);

                if (parentSystem == null)
                    throw new NotFoundException($"Parent system '{parent}' not found.");

                normalizedParents.Add(parent);
            }

            service.RunsOn = normalizedParents;
        }

        if (notes != null) service.Notes = notes;

        await repository.UpdateAsync(service);
    }
}
