using RackPeek.Domain.Persistence;

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
    List<string>? RunsOnSystemHost,
    List<string>? RunsOnPhysicalHost
);

public class ServiceReportUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<ServiceReport> ExecuteAsync()
    {
        var services = await repository.GetAllOfTypeAsync<Service>();

        var rows = services.Select(async s =>
        {
            List<string> runsOnPhysicalHost = new List<string>();
            if (s.RunsOn is not null)
            {
                foreach (var system in s.RunsOn)
                {
                    var systemResource = await repository.GetByNameAsync(system);
                    if (systemResource?.RunsOn is not null)
                    {
                        foreach (var parent in systemResource.RunsOn)
                        {
                            if (!runsOnPhysicalHost.Contains(parent)) runsOnPhysicalHost.Add(parent);
                        }
                    }
                }
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
