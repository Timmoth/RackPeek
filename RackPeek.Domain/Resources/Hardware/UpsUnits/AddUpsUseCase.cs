using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public class AddUpsUseCase(IHardwareRepository repository, IResourceRepository resourceRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var existingResourceKind = await resourceRepo.GetResourceKindAsync(name);
        if (!string.IsNullOrEmpty(existingResourceKind))
            throw new ConflictException($"{existingResourceKind} resource '{name}' already exists.");

        var ups = new Ups
        {
            Name = name
        };

        await repository.AddAsync(ups);
    }
}