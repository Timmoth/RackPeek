namespace RackPeek.Domain.Resources.Hardware.Server;

public class GetServerUseCase(IHardwareRepository repository)
{
    public async Task<Models.Server?> ExecuteAsync(string name)
    {
        var hardware = await repository.GetByNameAsync(name);
        return hardware as Models.Server;
    }
}