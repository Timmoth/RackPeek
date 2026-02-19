using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public class UpdateAccessPointUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        double? speed = null,
        string? notes = null
    )
    {
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
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

        if (notes != null)
        {
            ap.Notes = notes;
        }

        await repository.UpdateAsync(ap);
    }
}