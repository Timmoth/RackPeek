using System.Text;

namespace RackPeek.Domain.Persistence.Yaml;

public interface ITextFileStore {
    Task<bool> ExistsAsync(string path);
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string contents);
}

public sealed class PhysicalTextFileStore : ITextFileStore {
    public Task<bool> ExistsAsync(string path) => Task.FromResult(File.Exists(path));

    public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);

    /// <summary>
    ///     Atomic and durable replacement for File.WriteAllTextAsync, which truncates
    ///     the destination before writing and never flushes to disk — so a crash or
    ///     power loss mid-save could leave a truncated config, and a save that had
    ///     "succeeded" could still be lost (#337). The content is written to a
    ///     temporary file in the same directory, flushed to disk, then moved over the
    ///     destination — a rename, so readers only ever see the old or the new file,
    ///     never a partial one.
    /// </summary>
    public async Task WriteAllTextAsync(string path, string contents) {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new IOException($"'{path}' has no parent directory.");

        var tempPath = Path.Combine(
            directory,
            $"{Path.GetFileName(fullPath)}.tmp-{Guid.NewGuid():N}");

        try {
            await using (var stream = new FileStream(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None)) {
                var bytes = Encoding.UTF8.GetBytes(contents);
                await stream.WriteAsync(bytes);

                // Flush through the OS cache to the disk itself, so the rename
                // below never publishes a file whose bytes could still vanish.
                stream.Flush(true);
            }

            File.Move(tempPath, fullPath, true);
        }
        catch {
            // Never leave temp files behind on a failed write; the destination
            // is untouched by construction.
            try {
                File.Delete(tempPath);
            }
            catch (IOException) {
                // Best effort — the stray temp file is harmless.
            }

            throw;
        }
    }
}
