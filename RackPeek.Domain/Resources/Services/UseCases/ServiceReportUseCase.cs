using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Services.UseCases;

public record ServiceReport(
    IReadOnlyList<ServiceReportRow> Services
);

public record ServiceReportRow(
    string Name,
    string? Ip,
    int? Port,
    string? Protocol,
    string? Url,
    string? RunsOnSystemHost,
    string? RunsOnPhysicalHost
);

public class ServiceReportUseCase(IServiceRepository repository, ISystemRepository systemRepo) : IUseCase
{
    public async Task<ServiceReport> ExecuteAsync()
    {
        var services = await repository.GetAllAsync();

        var rows = services.Select(async s =>
        {
            string? runsOnPhysicalHost = null;
            if (!string.IsNullOrEmpty(s.RunsOn))
            {
                var systemResource = await systemRepo.GetByNameAsync(s.RunsOn);
                runsOnPhysicalHost = systemResource?.RunsOn;
            }

            return new ServiceReportRow(
                s.Name,
                s.Network?.Ip,
                s.Network?.Port,
                s.Network?.Protocol,
                s.Network?.Url,
                s.RunsOn,
                runsOnPhysicalHost
            );
        }).ToList();

        var result = await Task.WhenAll(rows);
        return new ServiceReport(result);
    }
}