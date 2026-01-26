namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public record SystemReport(
    IReadOnlyList<SystemHardwareRow> Systems
);

public record SystemHardwareRow(
    string Name,
    string? Type,
    string? Os,
    int Cores,
    int RamGb,
    int TotalStorageGb,
    string? RunsOn
);

public class SystemReportUseCase(ISystemRepository repository)
{
    public async Task<SystemReport> ExecuteAsync()
    {
        var systems = await repository.GetAllAsync();

        var rows = systems.Select(system =>
        {
            var totalStorage = system.Drives?.Sum(d => d.Size) ?? 0;

            return new SystemHardwareRow(
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
