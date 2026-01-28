namespace RackPeek.Domain.Resources.SystemResources.UseCases;

public class DeleteSystemUseCase(ISystemRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        if (await repository.GetByNameAsync(name) is not SystemResource)
            throw new InvalidOperationException($"System '{name}' not found.");

        await repository.DeleteAsync(name);
    }
}