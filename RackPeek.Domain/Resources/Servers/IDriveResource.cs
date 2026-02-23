using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Resources.Servers;

public interface IDriveResource
{
    public List<Drive>? Drives { get; set; }
}