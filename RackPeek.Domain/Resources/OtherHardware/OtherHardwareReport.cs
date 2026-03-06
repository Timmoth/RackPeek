using RackPeek.Domain.Persistence;

namespace RackPeek.Domain.Resources.OtherHardware;

public record OtherHardwareReport(
    IReadOnlyList<OtherHardwareRow> Others
);

public record OtherHardwareRow(
    string Name,
    string Model,
    string Description
);

public class OtherHardwareReportUseCase(IResourceCollection repository) : IUseCase
{
    public async Task<OtherHardwareReport> ExecuteAsync()
    {
        var others = await repository.GetAllOfTypeAsync<Other>();

        var rows = others.Select(o => new OtherHardwareRow(
            o.Name,
            o.Model ?? "Unknown",
            o.Description ?? ""
        )).ToList();

        return new OtherHardwareReport(rows);
    }
}
