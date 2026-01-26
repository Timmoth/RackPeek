namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class GetSystemUseCase(ISystemRepository repository)
{
    public async Task<SystemResource?> ExecuteAsync(string name)
    {
        return await repository.GetByNameAsync(name);
    }
}