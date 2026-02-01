using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Diagram.UseCases;

public class GenerateDiagramUseCase : IGenerateDiagramUseCase
{
    private readonly IHardwareRepository _hardware;
    private readonly ISystemRepository _systems;
    private readonly IServiceRepository _services;
    private readonly IDiagramRenderer _renderer;

    public GenerateDiagramUseCase(
        IHardwareRepository hardware,
        ISystemRepository systems,
        IServiceRepository services,
        IDiagramRenderer renderer)
    {
        _hardware = hardware;
        _systems = systems;
        _services = services;
        _renderer = renderer;
    }

    public async Task<string> ExecuteAsync()
    {
        var hardware = await _hardware.GetAllAsync();
        var systems = await _systems.GetAllAsync();
        var services = await _services.GetAllAsync();

        return _renderer.Render(hardware, systems, services);
    }
}