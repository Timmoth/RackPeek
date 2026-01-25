namespace RackPeek.Domain.Resources.Hardware.Server;

public class AddServerUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(string name)
    {
        // basic guard rails
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new InvalidOperationException($"Server '{name}' already exists.");

        var server = new Models.Server
        {
            Name = name,
        };

        await repository.AddAsync(server);
    }
}