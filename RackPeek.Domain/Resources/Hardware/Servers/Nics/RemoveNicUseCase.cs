using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.Hardware.Servers.Nics;

public class RemoveNicUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server)
            throw new NotFoundException($"Server: '{name}' not found.");

        server.Nics ??= [];

        if (index < 0 || index >= server.Nics.Count)
            throw new ValidationException("NIC index out of range.");

        server.Nics.RemoveAt(index);

        await repository.UpdateAsync(server);
    }
}