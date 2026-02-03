namespace RackPeek.Domain.Resources;

public interface IResourceRepository
{
    public Task<string?> GetResourceKindAsync(string name);
    public Task<bool> ResourceExistsAsync(string name);

}