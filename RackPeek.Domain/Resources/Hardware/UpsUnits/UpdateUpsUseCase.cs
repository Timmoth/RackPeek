using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public class UpdateUpsUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model = null,
        int? va = null,
        string? notes = null
    )
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase, ensure optional inputs are nullable
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var ups = await repository.GetByNameAsync(name) as Ups;
        if (ups == null)
            throw new InvalidOperationException($"UPS '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(model))
            ups.Model = model;

        if (va.HasValue)
            ups.Va = va.Value;
        if (notes != null)
        {
            ups.Notes = notes;
        }
        await repository.UpdateAsync(ups);
    }
}