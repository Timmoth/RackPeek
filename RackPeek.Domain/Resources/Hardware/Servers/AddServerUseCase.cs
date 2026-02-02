using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers;

public class AddServerUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        // basic guard rails
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new ConflictException($"Resource: '{name}' already exists.");

        var server = new Server
        {
            Name = name
        };

        await repository.AddAsync(server);
    }
}