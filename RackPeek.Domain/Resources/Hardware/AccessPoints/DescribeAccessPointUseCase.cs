using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.AccessPoints;

public record AccessPointDescription(
    string Name,
    string? Model,
    double? Speed
);

public class DescribeAccessPointUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<AccessPointDescription> ExecuteAsync(string name)
    {
        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);
        var ap = await repository.GetByNameAsync(name) as AccessPoint;
        if (ap == null)
            throw new NotFoundException($"Access point '{name}' not found.");

        return new AccessPointDescription(
            ap.Name,
            ap.Model,
            ap.Speed
        );
    }
}