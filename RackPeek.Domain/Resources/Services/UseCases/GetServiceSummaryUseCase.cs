namespace RackPeek.Domain.Resources.Services.UseCases;

public sealed class AllServicesSummary
{
    public AllServicesSummary(int totalServices, int totalIpAddresses)
    {
        TotalServices = totalServices;
        TotalIpAddresses = totalIpAddresses;
    }

    public int TotalServices { get; }
    public int TotalIpAddresses { get; }
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