namespace RackPeek.Domain.Resources.Hardware;

public interface IHardwareRepository
{
    Task<IReadOnlyList<Models.Hardware>> GetAllAsync();
    Task AddAsync(Models.Hardware hardware);
    Task UpdateAsync(Models.Hardware hardware);
    Task DeleteAsync(string name);
    Task<Models.Hardware?> GetByNameAsync(string name);
    public Task<List<HardwareTree>> GetTreeAsync();
}

public class HardwareTree
{
    public required string HardwareName { get; set; }
    public required string Kind { get; set; }
    public required List<SystemTree> Systems {get; set;}
}

public class SystemTree
{
    public required string SystemName { get; set; }
    public required List<string> Services {get; set;}
}