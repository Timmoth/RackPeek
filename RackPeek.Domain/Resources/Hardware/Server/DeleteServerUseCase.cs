namespace RackPeek.Domain.Resources.Hardware.Server;

public class DeleteServerUseCase(IHardwareRepository repository)
{
    public async Task ExecuteAsync(string name)
    {
        var hardware = await repository.GetByNameAsync(name);
        if (hardware == null)
            throw new InvalidOperationException($"Server '{name}' not found.");

        await repository.DeleteAsync(name);
    }
}