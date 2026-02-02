using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public class AddAccessPointUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);

        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new ConflictException($"Access point '{name}' already exists.");

        var ap = new AccessPoint
        {
            Name = name
        };

        await repository.AddAsync(ap);
    }
}