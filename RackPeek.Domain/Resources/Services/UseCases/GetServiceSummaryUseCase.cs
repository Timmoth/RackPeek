namespace RackPeek.Domain.Resources.Services.UseCases;

public sealed class AllServicesSummary(int totalServices, int totalIpAddresses)
{
    public int TotalServices { get; } = totalServices;
    public int TotalIpAddresses { get; } = totalIpAddresses;
}

public class GetServiceSummaryUseCase(IServiceRepository repository) : IUseCase
{
    public async Task<AllServicesSummary> ExecuteAsync()
    {
        var serviceCountTask = repository.GetCountAsync();
        var ipAddressCountTask = repository.GetIpAddressCountAsync();

        await Task.WhenAll(serviceCountTask, ipAddressCountTask);

        return new AllServicesSummary(
            serviceCountTask.Result,
            ipAddressCountTask.Result
        );
    }
}