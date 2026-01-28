using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public class UpdateUpsUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        int? va = null
    )
    {
        var ups = await repository.GetByNameAsync(name) as Ups;
        if (ups == null)
            throw new InvalidOperationException($"UPS '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            ups.Model = model;

        if (va.HasValue)
            ups.Va = va.Value;

        await repository.UpdateAsync(ups);
    }
}