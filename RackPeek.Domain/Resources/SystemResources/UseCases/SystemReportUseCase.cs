namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public record SystemReport(
    IReadOnlyList<SystemReportRow> Systems
);

public record SystemReportRow(
    string Name,
    string? Type,
    string? Os,
    int Cores,
    int RamGb,
    int TotalStorageGb,
    string? RunsOn
);

public class SystemReportUseCase(ISystemRepository repository) : IUseCase
{
    public async Task<SystemReport> ExecuteAsync()
    {
        var systems = await repository.GetAllAsync();

        var rows = systems.Select(system =>
        {
            var totalStorage = system.Drives?.Sum(d => d.Size) ?? 0;

            return new SystemReportRow(
                system.Name,
                system.Type,
                system.Os,
                system.Cores ?? 0,
                system.Ram ?? 0,
                totalStorage,
                system.RunsOn
            );
        }).ToList();

        return new SystemReport(rows);
    }
}