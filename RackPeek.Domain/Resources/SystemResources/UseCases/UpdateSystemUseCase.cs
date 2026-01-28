namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class UpdateSystemUseCase(ISystemRepository repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? type = null,
        string? os = null,
        int? cores = null,
        int? ram = null,
        string? runsOn = null
    )
    {
        var system = await repository.GetByNameAsync(name);
        if (system is null)
            throw new InvalidOperationException($"System '{name}' not found.");

        if (!string.IsNullOrWhiteSpace(type))
            system.Type = type;

        if (!string.IsNullOrWhiteSpace(os))
            system.Os = os;

        if (cores.HasValue)
            system.Cores = cores.Value;

        if (ram.HasValue)
            system.Ram = ram.Value;

        if (!string.IsNullOrWhiteSpace(runsOn))
            system.RunsOn = runsOn;

        await repository.UpdateAsync(system);
    }
}