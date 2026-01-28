namespace RackPeek.Domain.Resources.Services.UseCases;

public class GetServiceUseCase(IServiceRepository repository) : IUseCase
{
    public async Task<Service?> ExecuteAsync(string name)
    {
        return await repository.GetByNameAsync(name);
    }
}