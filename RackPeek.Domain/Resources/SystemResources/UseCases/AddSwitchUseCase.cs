namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class AddSystemUseCase(ISystemRepository repository)
{
    public async Task ExecuteAsync(string name)
    {
        // basic guard rails
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new InvalidOperationException($"System '{name}' already exists.");

        var system = new SystemResource
        {
            Name = name
        };

        await repository.AddAsync(system);
    }
}