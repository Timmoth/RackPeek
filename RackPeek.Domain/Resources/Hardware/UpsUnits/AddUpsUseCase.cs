using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public class AddUpsUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new InvalidOperationException($"UPS '{name}' already exists.");

        var ups = new Ups
        {
            Name = name
        };

        await repository.AddAsync(ups);
    }
}