namespace RackPeek.Domain.Persistence.Yaml;

public interface ITextFileStore
{
    Task<bool> ExistsAsync(string path);
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string contents);
}

public sealed class PhysicalTextFileStore : ITextFileStore
{
    public Task<bool> ExistsAsync(string path)
    {
        return Task.FromResult(File.Exists(path));
    }

    public Task<string> ReadAllTextAsync(string path)
    {
        return File.ReadAllTextAsync(path);
    }

    public Task WriteAllTextAsync(string path, string contents)
    {
        return File.WriteAllTextAsync(path, contents);
    }
}