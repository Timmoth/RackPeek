using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public class UpdateAccessPointUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        double? speed = null
    )
    {
        var ap = await repository.GetByNameAsync(name) as AccessPoint;
        if (ap == null)
            throw new InvalidOperationException($"Access point '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            ap.Model = model;

        if (speed.HasValue)
            ap.Speed = speed.Value;

        await repository.UpdateAsync(ap);
    }
}