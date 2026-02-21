using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.UpsUnits;

public record UpsDescription(
    string Name,
    string? Model,
    int? Va
);

public class DescribeUpsUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<UpsDescription> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var ups = await repository.GetByNameAsync(name) as Ups;
        if (ups == null)
            throw new NotFoundException($"Ups '{name}' not found.");

        return new UpsDescription(
            ups.Name,
            ups.Model,
            ups.Va
        );
    }
}