using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.Laptops;

public class GetLaptopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<Laptop> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);
        if (hardware is not Laptop laptop)
        {
            throw new NotFoundException($"Laptop '{name}' not found.");
        }

        return laptop;
    }
}