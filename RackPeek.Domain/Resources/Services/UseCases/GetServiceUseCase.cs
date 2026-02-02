using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Resources.Services.UseCases;

public class GetServiceUseCase(IServiceRepository repository) : IUseCase
{
    public async Task<Service?> ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);
        return await repository.GetByNameAsync(name);
    }
}