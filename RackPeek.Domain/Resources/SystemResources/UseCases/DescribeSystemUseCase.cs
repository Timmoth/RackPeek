using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public record SystemDescription(
    string Name,
    string? Type,
    string? Os,
    int Cores,
    double RamGb,
    int TotalStorageGb,
    string? RunsOn
);

public class DescribeSystemUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<SystemDescription> ExecuteAsync(string name)
    {
        name = Normalize.SystemName(name);
        ThrowIfInvalid.ResourceName(name);
        var system = await repository.GetByNameAsync(name) as SystemResource;
        if (system is null)
            throw new NotFoundException($"System '{name}' not found.");

        return new SystemDescription(
            system.Name,
            system.Type,
            system.Os,
            system.Cores ?? 0,
            system.Ram ?? 0,
            system.Drives?.Sum(d => d.Size) ?? 0,
            system.RunsOn
        );
    }
}