using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.OtherHardware;

public record OtherDescription(
    string Name,
    string? Model,
    string? Description,
    Dictionary<string, string> Labels
);

public class DescribeOtherUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<OtherDescription> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var other = await repository.GetByNameAsync(name) as Other;
        if (other == null)
            throw new NotFoundException($"Other hardware '{name}' not found.");

        return new OtherDescription(
            other.Name,
            other.Model,
            other.Description,
            other.Labels
        );
    }
}
