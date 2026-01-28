namespace RackPeek.Domain.Resources.Services.UseCases;

public class GetServicesUseCase(IServiceRepository repository) : IUseCase
{
    public async Task<IReadOnlyList<Service>> ExecuteAsync()
    {
        return await repository.GetAllAsync();
    }
}