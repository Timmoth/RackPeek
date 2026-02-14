using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Resources.Hardware;

public interface IHardwareRepository : IResourceRepo<Models.Hardware> 
{
    Task<int> GetCountAsync();
    Task<Dictionary<string, int>> GetKindCountAsync();
    
    public Task<List<HardwareTree>> GetTreeAsync();
}

public class HardwareTree
{
    public required string HardwareName { get; set; }
    public required string Kind { get; set; }
    public required List<SystemTree> Systems { get; set; }
}

public class SystemTree
{
    public required string SystemName { get; set; }
    public required List<string> Services { get; set; }
}