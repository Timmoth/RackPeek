using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Diagram.UseCases;

public class GenerateDiagramUseCase(
    IHardwareRepository hardwareRepo,
    ISystemRepository systemsRepo,
    IServiceRepository servicesRepo)
    : IUseCase
{
    public async Task<string> ExecuteAsync()
    {
        var hardware = await hardwareRepo.GetAllAsync();
        var systems = await systemsRepo.GetAllAsync();
        var services = await servicesRepo.GetAllAsync();

        return DrawioDiagramRenderer.RenderPhysicalServerGrouping(hardware, systems, services);
    }
}