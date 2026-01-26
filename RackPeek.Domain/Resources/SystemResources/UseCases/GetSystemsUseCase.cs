namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class GetSystemsUseCase(ISystemRepository repository)
{
    public async Task<IReadOnlyList<SystemResource>> ExecuteAsync()
    {
        return await repository.GetAllAsync();
    }
}
