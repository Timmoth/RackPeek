namespace RackPeek.Domain.Resources.Hardware.Desktops;

public class DeleteDesktopUseCase(IHardwareRepository repository) : IUseCase
{
    public async Task ExecuteAsync(string name)
    {
        var hardware = await repository.GetByNameAsync(name);
        if (hardware == null)
            throw new InvalidOperationException($"Desktop '{name}' not found.");

        await repository.DeleteAsync(name);
    }
}