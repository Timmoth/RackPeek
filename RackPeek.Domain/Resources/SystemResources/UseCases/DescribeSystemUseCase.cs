namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public record SystemDescription(
    string Name,
    string? Type,
    string? Os,
    int Cores,
    int RamGb,
    int TotalStorageGb,
    string? RunsOn
);

public class DescribeSystemUseCase(ISystemRepository repository)
{
    public async Task<SystemDescription?> ExecuteAsync(string name)
    {
        var system = await repository.GetByNameAsync(name);
        if (system is null)
            return null;

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
