using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class AddServiceUseCase(IServiceRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        name = Normalize.ServiceName(name);
        ThrowIfInvalid.ResourceName(name);
        // basic guard rails
        var existing = await repository.GetByNameAsync(name);
        if (existing != null)
            throw new InvalidOperationException($"Service '{name}' already exists.");

        var service = new Service
        {
            Name = name
        };

        await repository.AddAsync(service);
    }
}