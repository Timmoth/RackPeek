using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class UpdateServerUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int? ramGb = null,
        int? ramMts = null,
        bool? ipmi = null,
        string? notes = null
    )
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var server = await repository.GetByNameAsync(name) as Server;
        if (server == null)
            throw new NotFoundException($"Server '{name}' not found.");

        // ---- RAM ----
        if (ramGb.HasValue)
        {
            ThrowIfInvalid.RamGb(ramGb);
            server.Ram ??= new Ram();
            server.Ram.Size = ramGb.Value;
        }

        if (ramMts.HasValue)
        {
            server.Ram ??= new Ram();
            server.Ram.Mts = ramMts.Value;
        }

        // ---- IPMI ----
        if (ipmi.HasValue) server.Ipmi = ipmi.Value;
        if (notes != null) server.Notes = notes;
        await repository.UpdateAsync(server);
    }
}