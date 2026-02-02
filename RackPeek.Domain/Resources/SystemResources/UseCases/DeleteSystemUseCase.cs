using RackPeek.Domain.Helpers;
using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class DeleteSystemUseCase(ISystemRepository repository, IServiceRepository serviceRepo) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        ThrowIfInvalid.ResourceName(name);
        
        if (await repository.GetByNameAsync(name) is not SystemResource)
            throw new NotFoundException($"System '{name}' not found.");
        
        // Break link to dependants
        var dependants = await serviceRepo.GetBySystemHostAsync(name);
        foreach (var serviceResource in dependants)
        {
            serviceResource.RunsOn = null;
            await serviceRepo.UpdateAsync(serviceResource);
        }
        
        await repository.DeleteAsync(name);
    }
}