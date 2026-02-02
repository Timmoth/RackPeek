using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public class UpdateAccessPointUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        double? speed = null
    )
    {
        ThrowIfInvalid.ResourceName(name);

        var ap = await repository.GetByNameAsync(name) as AccessPoint;
        if (ap == null)
            throw new NotFoundException($"Access point '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
        {
            ThrowIfInvalid.AccessPointModelName(model);
            ap.Model = model;
        }

        if (speed.HasValue)
        {
            ThrowIfInvalid.NetworkSpeed(speed.Value);
            ap.Speed = speed.Value;
        }

        await repository.UpdateAsync(ap);
    }
}