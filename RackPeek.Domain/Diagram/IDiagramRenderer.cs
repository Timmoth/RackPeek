using RackPeek.Domain.Resources.Hardware.Models;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Diagram;

public interface IDiagramRenderer
{
    string Render(
        IReadOnlyList<Hardware> hardware,
        IReadOnlyList<SystemResource> systems,
        IReadOnlyList<Service> services);
}