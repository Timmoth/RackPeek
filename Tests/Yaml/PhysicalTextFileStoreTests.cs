using RackPeek.Domain.Persistence.Yaml;

namespace Tests.Yaml;

/// <summary>
///     Writer-side half of https://github.com/Timmoth/RackPeek/issues/337.
///     The store used to be a bare File.WriteAllTextAsync, which truncates the
///     destination before writing a byte and never flushes — so an interrupted save
///     could leave a truncated config, and a save that had returned successfully
///     could still be lost to power failure. It now writes to a temp file, flushes
///     to disk, and renames over the destination.
/// </summary>
public class PhysicalTextFileStoreTests : IDisposable {
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(),
        "rackpeek-store-tests",
        Guid.NewGuid().ToString("N"));

    private readonly PhysicalTextFileStore _store = new();

    public PhysicalTextFileStoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose() {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);

        GC.SuppressFinalize(this);
    }

    private string Path_(string name) => Path.Combine(_dir, name);

    [Fact]
    public async Task writing_a_new_file_round_trips_the_content() {
        var path = Path_("config.yaml");

        await _store.WriteAllTextAsync(path, "version: 4\n");

        Assert.Equal("version: 4\n", await _store.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task overwriting_replaces_the_whole_file() {
        var path = Path_("config.yaml");

        await _store.WriteAllTextAsync(path, new string('a', 4096));
        await _store.WriteAllTextAsync(path, "short");

        // A rename replaces the file wholesale; a partial in-place write would
        // leave the tail of the longer content behind.
        Assert.Equal("short", await _store.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task writing_leaves_no_temp_files_behind() {
        var path = Path_("config.yaml");

        for (var i = 0; i < 5; i++)
            await _store.WriteAllTextAsync(path, $"version: 4 # {i}\n");

        Assert.Equal(new[] { "config.yaml" },
            Directory.GetFiles(_dir).Select(System.IO.Path.GetFileName).OrderBy(n => n).ToArray());
    }

    [Fact]
    public async Task a_reader_never_observes_a_truncated_file_during_writes() {
        var path = Path_("config.yaml");

        // Two sizes, neither a prefix of the other: any partially written state is
        // detectable as "not equal to either of the two valid contents".
        var big = "version: 4\n" + new string('b', 200_000);
        var small = "version: 4\n" + new string('s', 50_000);

        await _store.WriteAllTextAsync(path, big);

        using var cts = new CancellationTokenSource();

        var writer = Task.Run(async () => {
            for (var i = 0; i < 40; i++)
                await _store.WriteAllTextAsync(path, i % 2 == 0 ? small : big);

            await cts.CancelAsync();
        });

        var observations = 0;

        while (!cts.IsCancellationRequested) {
            string seen;

            try {
                seen = await File.ReadAllTextAsync(path);
            }
            catch (IOException) {
                // The rename can momentarily deny sharing on Windows; not a torn read.
                continue;
            }

            observations++;

            Assert.True(seen == big || seen == small,
                $"Observed a partially written config ({seen.Length} bytes; expected {big.Length} or {small.Length}).");
        }

        await writer;

        Assert.True(observations > 0, "The reader never managed to sample the file.");
    }

    [Fact]
    public async Task a_failed_write_leaves_the_original_intact() {
        // A directory standing where the temp file wants to be makes the write fail
        // after the destination would have been truncated by the old implementation.
        var path = Path_("config.yaml");
        await _store.WriteAllTextAsync(path, "version: 4\nresources: []\n");

        var readOnlyDir = Path_("locked");
        Directory.CreateDirectory(readOnlyDir);
        var nested = Path.Combine(readOnlyDir, "config.yaml");
        await _store.WriteAllTextAsync(nested, "version: 4\n");

        // Writing to a path that is itself a directory always fails.
        var directoryPath = Path_("a-directory");
        Directory.CreateDirectory(directoryPath);

        await Assert.ThrowsAnyAsync<Exception>(
            () => _store.WriteAllTextAsync(directoryPath, "anything"));

        // The unrelated config is untouched, and no temp debris was left anywhere.
        Assert.Equal("version: 4\nresources: []\n", await _store.ReadAllTextAsync(path));
        Assert.DoesNotContain(Directory.GetFiles(_dir), f => f.Contains(".tmp-", StringComparison.Ordinal));
    }
}
