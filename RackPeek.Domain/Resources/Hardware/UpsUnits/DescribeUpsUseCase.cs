using RackPeek.Domain.Resources.Hardware.Models;

namespace RackPeek.Domain.Resources.Hardware.UpsUnits;

public record UpsDescription(
    string Name,
    string? Model,
    int? Va
);

public class DescribeUpsUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task<UpsDescription?> ExecuteAsync(string name)
    {
        var ups = await repository.GetByNameAsync(name) as Ups;
        if (ups == null)
            return null;

        return new UpsDescription(
            ups.Name,
            ups.Model,
            ups.Va
        );
    }
}