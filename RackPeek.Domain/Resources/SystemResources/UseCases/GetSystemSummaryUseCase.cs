namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public sealed class SystemSummary
{
    public SystemSummary(
        int totalSystems,
        IReadOnlyDictionary<string, int> systemsByType,
        IReadOnlyDictionary<string, int> systemsByOs)
    {
        TotalSystems = totalSystems;
        SystemsByType = systemsByType;
        SystemsByOs = systemsByOs;
    }

    public int TotalSystems { get; }
    public IReadOnlyDictionary<string, int> SystemsByType { get; }
    public IReadOnlyDictionary<string, int> SystemsByOs { get; }
}

public class GetSystemSummaryUseCase(ISystemRepository repository) : IUseCase
{
    public async Task<SystemSummary> ExecuteAsync()
    {
        var totalSystemsTask = repository.GetSystemCountAsync();
        var systemsByTypeTask = repository.GetSystemTypeCountAsync();
        var systemsByOsTask = repository.GetSystemOsCountAsync();

        await Task.WhenAll(
            totalSystemsTask,
            systemsByTypeTask,
            systemsByOsTask
        );

        return new SystemSummary(
            totalSystemsTask.Result,
            systemsByTypeTask.Result,
            systemsByOsTask.Result
        );
    }
}