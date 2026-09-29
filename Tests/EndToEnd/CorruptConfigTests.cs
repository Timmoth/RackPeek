using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd;

// Load-side half of https://github.com/Timmoth/RackPeek/issues/337.
//
// Before the fix a damaged config was accepted without complaint: an unparseable
// file loaded as an EMPTY inventory with exit 0, and the next write persisted that
// emptiness over a recoverable file. These tests pin the contract that a config
// which exists but cannot be understood fails loudly on every read, and — the part
// that actually loses data — is never overwritten.
//
// Note on what is NOT testable here: a save interrupted at a clean resource boundary
// leaves valid YAML that is indistinguishable from a smaller inventory. Nothing at
// load time can detect it, which is precisely why the writer-side fix (atomic,
// durable saves — see PhysicalTextFileStoreTests) is the primary remedy.
[Collection("Process-wide static state")]
public class CorruptConfigTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    // Exactly what the serializer writes for three bare servers.
    private const string _fullConfig = """
                                       version: 4
                                       resources:
                                       - kind: Server
                                         name: srv-a
                                       - kind: Server
                                         name: srv-b
                                       - kind: Server
                                         name: srv-c
                                       connections: []

                                       """;

    private async Task<string> ExecuteAsync(params string[] args) {
        outputHelper.WriteLine($"rpk {string.Join(" ", args)}");

        var output = await YamlCliTestHost.RunAsync(
            args,
            fs.Root,
            outputHelper,
            "config.yaml");

        outputHelper.WriteLine(output);
        return output;
    }

    private string ConfigPath => Path.Combine(fs.Root, "config.yaml");

    [Fact]
    public async Task a_config_cut_mid_token_fails_with_a_friendly_error() {
        // A save that died mid-write inside a YAML token.
        var truncated = _fullConfig[.._fullConfig.IndexOf("nd: Server\n  name: srv-c", StringComparison.Ordinal)];
        await File.WriteAllTextAsync(ConfigPath, truncated);

        var output = await ExecuteAsync("summary");

        // An actionable message naming the config file — not a stack dump, and above
        // all not a cheerful "Hardware (0)".
        Assert.Contains("config.yaml", output);
        Assert.DoesNotContain("at RackPeek.", output);
        Assert.DoesNotContain("YamlDotNet.Core", output);
        Assert.DoesNotContain("Hardware (0)", output);
    }

    [Fact]
    public async Task a_file_that_is_not_a_rackpeek_config_is_rejected() {
        // Parses as YAML, carries no schema version: not our document.
        await File.WriteAllTextAsync(ConfigPath, "hello: world\n");

        var output = await ExecuteAsync("summary");

        Assert.Contains("config.yaml", output);
        Assert.DoesNotContain("Hardware (0)", output);
    }

    [Fact]
    public async Task a_damaged_config_is_never_overwritten_by_a_later_write() {
        // The data-loss path: read the damaged file, then try to write. The write
        // must refuse rather than persist the empty in-memory collection over it.
        var truncated = _fullConfig[.._fullConfig.IndexOf("nd: Server\n  name: srv-c", StringComparison.Ordinal)];
        await File.WriteAllTextAsync(ConfigPath, truncated);

        var output = await ExecuteAsync("servers", "add", "srv-d");

        Assert.DoesNotContain("added", output, StringComparison.OrdinalIgnoreCase);

        var onDisk = await File.ReadAllTextAsync(ConfigPath);
        Assert.Equal(truncated, onDisk);
    }

    [Fact]
    public async Task an_empty_config_is_still_a_valid_empty_inventory() {
        // The file the CLI itself creates on first run. Must not be mistaken for damage.
        await File.WriteAllTextAsync(ConfigPath, "");

        var output = await ExecuteAsync("servers", "add", "srv-a");

        Assert.Contains("added", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name: srv-a", await File.ReadAllTextAsync(ConfigPath));
    }

    [Fact]
    public async Task a_legacy_config_without_a_version_key_still_migrates() {
        // Pre-v1 files carry no version key at all. The migration chain stamps one,
        // so they must not trip the "missing schema version" guard.
        await File.WriteAllTextAsync(
            ConfigPath,
            "resources:\n  - kind: Server\n    name: legacy-srv\n");

        var output = await ExecuteAsync("summary");

        Assert.Contains("Server: 1", output);
        Assert.Contains("version: 4", await File.ReadAllTextAsync(ConfigPath));
    }

    [Fact]
    public async Task a_healthy_config_still_loads_and_writes() {
        await File.WriteAllTextAsync(ConfigPath, _fullConfig);

        var output = await ExecuteAsync("summary");
        Assert.Contains("Server: 3", output);

        output = await ExecuteAsync("servers", "add", "srv-d");
        Assert.Contains("added", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name: srv-d", await File.ReadAllTextAsync(ConfigPath));
    }
}
