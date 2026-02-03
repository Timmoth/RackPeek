using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class GetServiceUseCase(IServiceRepository repository) : IUseCase
{
    public async Task<Service> ExecuteAsync(string name)
    {
        name = Normalize.ServiceName(name);
        ThrowIfInvalid.ResourceName(name);
        var resource = await repository.GetByNameAsync(name);

        if (resource is null)
        {
            throw new NotFoundException($"Service '{name}' not found.");
        }
        
        return resource;
    }
}