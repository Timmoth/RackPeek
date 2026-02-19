using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Cpus;

public class AddCpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        string? model,
        int? cores,
        int? threads)
    {
        // ToDo pass in properties as inputs, construct the entity in the usecase
        // ToDo validate / normalize all inputs

        name = Normalize.HardwareName(name);
        ThrowIfInvalid.ResourceName(name);

        var hardware = await repository.GetByNameAsync(name);

        if (hardware is not Server server) return;

        server.Cpus ??= [];

        server.Cpus.Add(new Cpu
        {
            Model = model,
            Cores = cores,
            Threads = threads
        });

        await repository.UpdateAsync(server);
    }
}