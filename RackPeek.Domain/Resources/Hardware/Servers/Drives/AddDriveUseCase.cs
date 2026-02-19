using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Drives;

public class AddDrivesUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? type,
        int? size)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server)
            throw new NotFoundException($"Server '{name}' not found.");

        server.Drives ??= [];

        server.Drives.Add(new Drive
        {
            Type = type,
            Size = size
        });

        await repository.UpdateAsync(server);
    }
}