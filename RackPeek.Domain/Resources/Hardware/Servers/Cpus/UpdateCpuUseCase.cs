using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources.Models;

namespace RackPeek.Domain.Resources.Hardware.Servers.Cpus;

public class UpdateCpuUseCase(IResourceCollection repository) : IUseCase
{
    public async Task ExecuteAsync(
        string name,
        int index,
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

        if (index < 0 || index >= server.Cpus.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "CPU index out of range.");

        var cpu = server.Cpus[index];
        cpu.Model = model;
        cpu.Cores = cores;
        cpu.Threads = threads;

        await repository.UpdateAsync(server);
    }
}