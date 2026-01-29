namespace RackPeek.Domain.Resources.Hardware;

public sealed class HardwareSummary
{
    public HardwareSummary(
        int totalHardware,
        IReadOnlyDictionary<string, int> hardwareByKind)
    {
        TotalHardware = totalHardware;
        HardwareByKind = hardwareByKind;
    }

    public int TotalHardware { get; }
    public IReadOnlyDictionary<string, int> HardwareByKind { get; }
}

public class GetHardwareUseCaseSummary(IHardwareRepository repository) : IUseCase
{
    public async Task<HardwareSummary> ExecuteAsync()
    {
        var totalCountTask = repository.GetCountAsync();
        var kindCountTask = repository.GetKindCountAsync();

        await Task.WhenAll(totalCountTask, kindCountTask);

        return new HardwareSummary(
            totalCountTask.Result,
            kindCountTask.Result
        );
    }
}