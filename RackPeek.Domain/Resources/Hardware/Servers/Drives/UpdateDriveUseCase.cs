using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Drives;

public class UpdateDriveUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(string name, int index, string? type, int? size)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Server server)
            throw new NotFoundException($"Server '{name}' not found.");


        server.Drives ??= [];
        if (index < 0 || index >= server.Drives.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Drive index out of range.");

        var drive = server.Drives[index];
        drive.Type = type;
        drive.Size = size;
        await repository.UpdateAsync(server);
    }
}